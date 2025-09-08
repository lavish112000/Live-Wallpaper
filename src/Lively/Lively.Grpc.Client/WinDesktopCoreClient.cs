using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Common.Exceptions;
using Lively.Common.Helpers.Storage;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    //TODO: don't catch exceptions, just throw.
    public class WinDesktopCoreClient : IDesktopCoreClient
    {
        public event EventHandler WallpaperChanged;
        public event EventHandler<Exception> WallpaperError;

        private readonly List<WallpaperData> wallpapers = new List<WallpaperData>(2);
        public ReadOnlyCollection<WallpaperData> Wallpapers => wallpapers.AsReadOnly();
        public string BaseDirectory { get; private set; }
        public Version AssemblyVersion { get; private set; }
        public bool IsCoreInitialized { get; private set; }

        private readonly DesktopService.DesktopServiceClient client;
        private readonly SemaphoreSlim wallpaperChangedLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource cancellationTokenWallpaperChanged, cancellationTokenWallpaperError;
        private readonly Task wallpaperChangedTask, wallpaperErrorTask;
        private bool disposedValue;

        public WinDesktopCoreClient()
        {
            client = new DesktopService.DesktopServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));

            Task.Run(async () =>
            {
                wallpapers.AddRange(await GetWallpapers().ConfigureAwait(false));
                var status = (await GetCoreStats().ConfigureAwait(false));
                BaseDirectory = status.BaseDirectory;
                AssemblyVersion = new Version(status.AssemblyVersion);
                IsCoreInitialized = status.IsCoreInitialized;
            }).Wait();

            cancellationTokenWallpaperChanged = new CancellationTokenSource();
            wallpaperChangedTask = Task.Run(() => SubscribeWallpaperChangedStream(cancellationTokenWallpaperChanged.Token));
            cancellationTokenWallpaperError = new CancellationTokenSource();
            wallpaperErrorTask = Task.Run(() => SubscribeWallpaperErrorStream(cancellationTokenWallpaperError.Token));
        }

        private async Task<GetCoreStatsResponse> GetCoreStats() => await client.GetCoreStatsAsync(new Empty());

        public async Task SetWallpaper(string livelyInfoPath, string monitorId)
        {
            var request = new SetWallpaperRequest
            {
                LivelyInfoPath = livelyInfoPath,
                MonitorId = monitorId,
                Type = LibraryItemCategory.Ready,
            };
            _ = await client.SetWallpaperAsync(request);
        }

        public async Task SetWallpaper(LibraryModel item, DisplayMonitor display)
        {
            var request = new SetWallpaperRequest
            {
                LivelyInfoPath = item.LivelyInfoFolderPath,
                MonitorId = display.DeviceId,
            };
            _ = await client.SetWallpaperAsync(request);
        }

        public async Task<bool> EditWallpaper(string livelyInfoPath)
        {
            var response = await client.EditWallpaperAsync(new EditWallpaperRequest() { 
                LivelyInfoPath = livelyInfoPath
            });

            if (response.Error != null)
                WallpaperError?.Invoke(this, GetException(response.Error));

            return response.IsSuccess;
        }

        public async Task<string> CreateWallpaper(string filePath, WallpaperType type, string arguments = null)
        {
            var response = await client.CreateWallpaperAsync(new CreateWallpaperRequest()
            {
                FilePath = filePath,
                Category = (WallpaperCategory)(int)type,
                Arguments = string.IsNullOrWhiteSpace(arguments) ? string.Empty : arguments,
            });

            if (response.Error != null)
                WallpaperError?.Invoke(this, GetException(response.Error));

            return response.IsSuccess ? response.LivelyInfoPath : null;
        }

        private async Task<List<WallpaperData>> GetWallpapers()
        {
            var resp = new List<GetWallpapersResponse>();
            using var call = client.GetWallpapers(new Empty());
            while (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                resp.Add(response);
            }

            var wallpapers = new List<WallpaperData>();
            foreach (var item in resp)
            {
                wallpapers.Add(new WallpaperData()
                {
                    LivelyInfoFolderPath = item.LivelyInfoPath,
                    LivelyPropertyCopyPath = item.PropertyCopyPath,
                    PreviewPath = item.PreviewPath,
                    ThumbnailPath = item.ThumbnailPath,
                    Category = (WallpaperType)(int)item.Category,
                    Display = new DisplayMonitor()
                    {
                        DeviceId = item.Screen.DeviceId,
                        DisplayName = item.Screen.DisplayName,
                        DeviceName = item.Screen.DeviceName,
                        HMonitor = new IntPtr(item.Screen.HMonitor),
                        IsPrimary = item.Screen.IsPrimary,
                        Index = item.Screen.Index,
                        Bounds = new System.Drawing.Rectangle(
                        item.Screen.Bounds.X,
                        item.Screen.Bounds.Y,
                        item.Screen.Bounds.Width,
                        item.Screen.Bounds.Height),
                        WorkingArea = new System.Drawing.Rectangle(
                        item.Screen.WorkingArea.X,
                        item.Screen.WorkingArea.Y,
                        item.Screen.WorkingArea.Width,
                        item.Screen.WorkingArea.Height),

                    },
                });
            }
            return wallpapers;
        }

        public async Task PreviewWallpaper(string livelyInfoPath)
        {
            await client.PreviewWallpaperAsync(new PreviewWallpaperRequest() { LivelyInfoPath = livelyInfoPath });
        }

        public async Task CloseAllWallpapers()
        {
            await client.CloseAllWallpapersAsync(new CloseAllWallpapersRequest() { });
        }

        public async Task CloseWallpaper(WallpaperType type)
        {
            await client.CloseWallpaperCategoryAsync(new CloseWallpaperCategoryRequest()
            {
                Category = (WallpaperCategory)((int)type)
            });
        }

        public async Task CloseWallpaper(LibraryModel item)
        {
            await client.CloseWallpaperLibraryAsync(new CloseWallpaperLibraryRequest()
            {
                LivelyInfoPath = item.LivelyInfoFolderPath
            });
        }

        public async Task CloseWallpaper(DisplayMonitor monitor)
        {
            await client.CloseWallpaperMonitorAsync(new CloseWallpaperMonitorRequest()
            {
                MonitorId = monitor.DeviceId
            });
        }

        public void SendMessageWallpaper(LibraryModel obj, IpcMessage msg)
        {
            client.SendMessageWallpaper(new WallpaperMessageRequest()
            {
                MonitorId = string.Empty,
                LivelyInfoPath = obj.LivelyInfoFolderPath,
                Msg = JsonUtil.Serialize(msg),
            });
        }

        public void SendMessageWallpaper(DisplayMonitor display, LibraryModel obj, IpcMessage msg)
        {
            client.SendMessageWallpaper(new WallpaperMessageRequest()
            {
                MonitorId = display.DeviceId,
                LivelyInfoPath = obj.LivelyInfoFolderPath,
                Msg = JsonUtil.Serialize(msg),
            });
        }

        public async Task TakeScreenshot(string monitorId, string savePath)
        {
            await client.TakeScreenshotAsync(new WallpaperScreenshotRequest()
            {
                MonitorId = monitorId,
                SavePath = savePath,
            });
        }

        private async Task SubscribeWallpaperChangedStream(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeWallpaperChanged(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    await wallpaperChangedLock.WaitAsync();
                    try
                    {
                        var response = call.ResponseStream.Current;

                        wallpapers.Clear();
                        wallpapers.AddRange(await GetWallpapers());
                        WallpaperChanged?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        wallpaperChangedLock.Release();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task SubscribeWallpaperErrorStream(CancellationToken token)
        {
            try
            {
                using var call = client.SubscribeWallpaperError(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    var response = call.ResponseStream.Current;

                    var exp = GetException(response);
                    WallpaperError?.Invoke(this, exp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static Exception GetException(WallpaperErrorResponse error)
        {
            return error.Error switch
            {
                ErrorCategory.Workerw => new WorkerWException(error.ErrorMsg),
                ErrorCategory.WallpaperNotFound => new WallpaperNotFoundException(error.ErrorMsg),
                ErrorCategory.WallpaperNotAllowed => new WallpaperNotAllowedException(error.ErrorMsg),
                ErrorCategory.WallpaperPluginNotFound => new WallpaperPluginNotFoundException(error.ErrorMsg),
                ErrorCategory.WallpaperPluginFail => new WallpaperPluginException(error.ErrorMsg),
                ErrorCategory.WallpaperPluginMediaCodecMissing => new WallpaperPluginMediaCodecException(error.ErrorMsg),
                ErrorCategory.ScreenNotFound => new ScreenNotFoundException(error.ErrorMsg),
                ErrorCategory.WallpaperWebview2NotFound => new WallpaperWebView2NotFoundException(error.ErrorMsg),
                ErrorCategory.WallpaperFileError => new WallpaperFileException(error.ErrorMsg),
                _ => new Exception(error.ErrorMsg),
            };
        }

        #region dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    cancellationTokenWallpaperChanged?.Cancel();
                    cancellationTokenWallpaperError?.Cancel();
                    Task.WaitAll(wallpaperChangedTask, wallpaperErrorTask);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DesktopServiceClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion //dispose
    }
}
