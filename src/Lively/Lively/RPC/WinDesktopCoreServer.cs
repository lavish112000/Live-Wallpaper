using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Common;
using Lively.Common.Exceptions;
using Lively.Common.Factories;
using Lively.Common.Helpers.Files;
using Lively.Common.JsonConverters;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Extensions;
using Lively.Factories;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.Models.Enums;
using Lively.Models.Message;
using Lively.Views;
using Newtonsoft.Json;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lively.RPC
{
    internal class WinDesktopCoreServer : DesktopService.DesktopServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IRunnerService runner;
        private readonly IDesktopCore desktopCore;
        private readonly IRunnerService runnerService;
        private readonly IDisplayManager displayManager;
        private readonly IUserSettingsService userSettings;
        private readonly IWindowService windowService;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly IWallpaperPluginFactory wallpaperFactory;

        public WinDesktopCoreServer(IDesktopCore desktopCore,
            IRunnerService runner,
            IRunnerService runnerService,
            IDisplayManager displayManager,
            IUserSettingsService userSettings,
            IWindowService windowService,
            IWallpaperPluginFactory wallpaperFactory,
            IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.runner = runner;
            this.desktopCore = desktopCore;
            this.runnerService = runnerService;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.windowService = windowService;
            this.wallpaperFactory = wallpaperFactory;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
        }

        public override Task<GetCoreStatsResponse> GetCoreStats(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new GetCoreStatsResponse()
            {
                BaseDirectory = AppDomain.CurrentDomain.BaseDirectory,
                AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                IsCoreInitialized = desktopCore.DesktopWorkerW != IntPtr.Zero,
            });
        }

        public override async Task<Empty> SetWallpaper(SetWallpaperRequest request, ServerCallContext context)
        {
            try
            {
                var lm = wallpaperLibraryFactory.CreateFromDirectory(request.LivelyInfoPath);
                var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
                await desktopCore.SetWallpaperAsync(lm, display ?? displayManager.PrimaryDisplayMonitor);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            return await Task.FromResult(new Empty());
        }

        public override async Task<CreateWallpaperResponse> CreateWallpaper(CreateWallpaperRequest request, ServerCallContext context)
        {
            bool isSuccess = false;
            IWallpaper wallpaper = null;
            WallpaperErrorResponse error = null;
            string wallpaperDirectory = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
            try
            {
                runner.SetBusyUI(true);
                _ = wallpaperLibraryFactory.CreateWallpaperPackage(request.FilePath, wallpaperDirectory, (WallpaperType)request.Category);
                var model = wallpaperLibraryFactory.CreateFromDirectory(wallpaperDirectory);
                wallpaper = wallpaperFactory.CreateWallpaper(model,
                    displayManager.PrimaryDisplayMonitor,
                    WallpaperArrangement.per,
                    isWindowed: true);

                // Closing since absolute location can be changed to relative.
                desktopCore.CloseWallpaper(model);
                await wallpaper.ShowAsync();
                isSuccess = await windowService.ShowWallpaperDialogWindowAsync(wallpaper);
            }
            catch (Exception ex)
            {
                error = new()
                {
                    ErrorMsg = ex.Message ?? string.Empty,
                    Error = GetError(ex)
                };
                Logger.Error(ex);
            }
            finally
            {
                runner.SetBusyUI(false);
                if (wallpaper != null)
                {
                    var propertyCopyPath = wallpaper.LivelyPropertyCopyPath;
                    wallpaper.Close();
                    wallpaper.Dispose();

                    if (!isSuccess)
                    {
                        try
                        {
                            await FileUtil.TryDeleteDirectoryAsync(wallpaperDirectory, 500, 0);
                            if (propertyCopyPath != null)
                            {
                                var propertiesDirectory = Directory.GetParent(Path.GetDirectoryName(propertyCopyPath)).FullName;
                                await FileUtil.TryDeleteDirectoryAsync(propertiesDirectory, 0, 500);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                }
            }

            return await Task.FromResult(new CreateWallpaperResponse()
            {
                IsSuccess = isSuccess,
                LivelyInfoPath = wallpaperDirectory ?? string.Empty,
                Error = error
            });
        }

        public override async Task<EditWallpaperResponse> EditWallpaper(EditWallpaperRequest request, ServerCallContext context)
        {
            var isSuccess = false;
            IWallpaper wallpaper = null;
            WallpaperErrorResponse error = null;
            try
            {
                runner.SetBusyUI(true);
                var model = wallpaperLibraryFactory.CreateFromDirectory(request.LivelyInfoPath);
                wallpaper = wallpaperFactory.CreateWallpaper(model,
                    displayManager.PrimaryDisplayMonitor,
                    WallpaperArrangement.per,
                    isWindowed: true);

                // Closing since absolute location can be changed to relative.
                desktopCore.CloseWallpaper(model);
                await wallpaper.ShowAsync();
                isSuccess = await windowService.ShowWallpaperDialogWindowAsync(wallpaper);
            }
            catch (Exception ex)
            {
                error = new()
                {
                    ErrorMsg = ex.Message ?? string.Empty,
                    Error = GetError(ex)
                };
                Logger.Error(ex);
            }
            finally 
            {
                runner.SetBusyUI(false);
                wallpaper?.Close();
                wallpaper?.Dispose();
            }

            return await Task.FromResult(new EditWallpaperResponse()
            {
                IsSuccess = isSuccess,
                Error = error
            });
        }

        public override async Task GetWallpapers(Empty _, IServerStreamWriter<GetWallpapersResponse> responseStream, ServerCallContext context)
        {
            try
            {
                foreach (var wallpaper in desktopCore.Wallpapers)
                {
                    var item = new GetWallpapersResponse()
                    {
                        LivelyInfoPath = wallpaper.Model.LivelyInfoFolderPath,
                        Screen = new ScreenData()
                        {
                            DeviceId = wallpaper.Screen.DeviceId,
                            DeviceName = wallpaper.Screen.DeviceName,
                            DisplayName = wallpaper.Screen.DisplayName,
                            HMonitor = wallpaper.Screen.HMonitor.ToInt32(),
                            IsPrimary = wallpaper.Screen.IsPrimary,
                            Index = wallpaper.Screen.Index,
                            WorkingArea = new Rectangle()
                            {
                                X = wallpaper.Screen.WorkingArea.X,
                                Y = wallpaper.Screen.WorkingArea.Y,
                                Width = wallpaper.Screen.WorkingArea.Width,
                                Height = wallpaper.Screen.WorkingArea.Height
                            },
                            Bounds = new Rectangle()
                            {
                                X = wallpaper.Screen.Bounds.X,
                                Y = wallpaper.Screen.Bounds.Y,
                                Width = wallpaper.Screen.Bounds.Width,
                                Height = wallpaper.Screen.Bounds.Height
                            }
                        },
                        ThumbnailPath = wallpaper.Model.ThumbnailPath ?? string.Empty,
                        PreviewPath = wallpaper.Model.PreviewClipPath ?? string.Empty,
                        PropertyCopyPath = wallpaper.LivelyPropertyCopyPath ?? string.Empty,
                        Category = (WallpaperCategory)(int)wallpaper.Category
                    };
                    await responseStream.WriteAsync(item);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public override Task<Empty> PreviewWallpaper(PreviewWallpaperRequest request, ServerCallContext context)
        {
            try
            {
                var lm = wallpaperLibraryFactory.CreateFromDirectory(request.LivelyInfoPath);
                _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                  {
                      var preview = new WallpaperPreview(lm, userSettings.Settings.SelectedDisplay, userSettings.Settings.WallpaperArrangement) {
                          // Default incase UI not running.
                          WindowStartupLocation = WindowStartupLocation.CenterScreen,
                      };
                      preview.Show();
                      // Center preview relative to UI.
                      if (runnerService.IsVisibleUI)
                          preview.CenterToWindow(runnerService.HwndUI);
                      // Re-activate incase launching wallpaper loses focus.
                      preview.Activate();
                  }));
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseAllWallpapers(CloseAllWallpapersRequest request, ServerCallContext context)
        {
            desktopCore.CloseAllWallpapers();
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperMonitor(CloseWallpaperMonitorRequest request, ServerCallContext context)
        {
            var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
            if (display != null)
            {
                desktopCore.CloseWallpaper(display);
            }
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperLibrary(CloseWallpaperLibraryRequest request, ServerCallContext context)
        {
            try
            {
                var lm = wallpaperLibraryFactory.CreateFromDirectory(request.LivelyInfoPath);
                desktopCore.CloseWallpaper(lm);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> CloseWallpaperCategory(CloseWallpaperCategoryRequest request, ServerCallContext context)
        {
            try
            {
                desktopCore.CloseWallpaper((WallpaperType)((int)request.Category));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            return Task.FromResult(new Empty());
        }

        public override async Task SubscribeWallpaperChanged(Empty _, IServerStreamWriter<Empty> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    desktopCore.WallpaperChanged += WallpaperChanged;
                    void WallpaperChanged(object s, EventArgs e)
                    {
                        desktopCore.WallpaperChanged -= WallpaperChanged;
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        desktopCore.WallpaperChanged -= WallpaperChanged;
                        break;
                    }

                    await responseStream.WriteAsync(new Empty());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public override async Task SubscribeWallpaperError(Empty _, IServerStreamWriter<WallpaperErrorResponse> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var resp = new WallpaperErrorResponse();
                    var tcs = new TaskCompletionSource<bool>();
                    desktopCore.WallpaperError += WallpaperError;
                    void WallpaperError(object s, Exception e)
                    {
                        desktopCore.WallpaperError -= WallpaperError;

                        resp.ErrorMsg = e.Message ?? string.Empty;
                        resp.Error = GetError(e);
                        tcs.TrySetResult(true);
                    }
                    using var item = context.CancellationToken.Register(() => { tcs.TrySetResult(false); });
                    await tcs.Task;

                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        desktopCore.WallpaperError -= WallpaperError;
                        break;
                    }

                    await responseStream.WriteAsync(resp);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public override Task<Empty> SendMessageWallpaper(WallpaperMessageRequest request, ServerCallContext context)
        {
            var obj = JsonConvert.DeserializeObject<IpcMessage>(request.Msg, new JsonSerializerSettings()
            {
                Converters = {
                    new IpcMessageConverter()
                }}
            );

            if (string.IsNullOrEmpty(request.MonitorId))
            {
                desktopCore.SendMessageWallpaper(request.LivelyInfoPath, obj);
            }
            else
            {
                var display = displayManager.DisplayMonitors.FirstOrDefault(x => x.DeviceId == request.MonitorId);
                desktopCore.SendMessageWallpaper(display, request.LivelyInfoPath, obj);
            }
            return Task.FromResult(new Empty());
        }

        public override async Task<Empty> TakeScreenshot(WallpaperScreenshotRequest request, ServerCallContext context)
        {
            try
            {
                switch (userSettings.Settings.WallpaperArrangement)
                {
                    case WallpaperArrangement.per:
                        {
                            var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => request.MonitorId == x.Screen.DeviceId);
                            if (wallpaper is not null)
                            {
                                await wallpaper.ScreenCapture(request.SavePath);
                            }
                        }
                        break;
                    case WallpaperArrangement.span:
                    case WallpaperArrangement.duplicate:
                        if (desktopCore.Wallpapers.Any())
                        {
                            await desktopCore.Wallpapers[0].ScreenCapture(request.SavePath);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                Logger.Error(e);
            }
            return await Task.FromResult(new Empty());
        }

        private static ErrorCategory GetError(Exception ex)
        {
            return ex switch
            {
                WorkerWException _ => ErrorCategory.Workerw,
                WallpaperNotAllowedException _ => ErrorCategory.WallpaperNotAllowed,
                WallpaperNotFoundException _ => ErrorCategory.WallpaperNotFound,
                WallpaperPluginException _ => ErrorCategory.WallpaperPluginFail,
                WallpaperPluginNotFoundException _ => ErrorCategory.WallpaperPluginNotFound,
                WallpaperPluginMediaCodecException _ => ErrorCategory.WallpaperPluginMediaCodecMissing,
                ScreenNotFoundException _ => ErrorCategory.ScreenNotFound,
                WallpaperWebView2NotFoundException _ => ErrorCategory.WallpaperWebview2NotFound,
                WallpaperFileException _ => ErrorCategory.WallpaperFileError,
                _ => ErrorCategory.General,
            };
        }
    }
}
