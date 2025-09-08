using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Core;
using Lively.Extensions;
using Lively.Factories;
using Lively.Models;
using Lively.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Lively.Views
{
    public partial class WallpaperPreview : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly LibraryModel model;
        private readonly DisplayMonitor display;
        private readonly WallpaperArrangement arrangement;
        private readonly TaskCompletionSource loadingTaskCompletionSource = new();
        private IWallpaper wallpaper;
        private bool _isInitialized = false;

        private readonly IWallpaperPluginFactory wallpaperFactory;
        private readonly IUserSettingsService userSettings;

        public WallpaperPreview(LibraryModel model,
            DisplayMonitor display,
            WallpaperArrangement arrangement,
            bool isAutoLoad = true)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            wallpaperFactory = App.Services.GetRequiredService<IWallpaperPluginFactory>();
            this.model = model;
            this.display = display;
            this.arrangement = arrangement;

            InitializeComponent();
            this.Title = model.Title;

            if (isAutoLoad)
                _ = LoadWallpaperAsync();
        }

        public async Task LoadWallpaperAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                await loadingTaskCompletionSource.Task;
                wallpaper = wallpaperFactory.CreateWallpaper(model,
                    display,
                    arrangement,
                    isWindowed: true);
                await wallpaper.ShowAsync();

                //Attach wp hwnd to border ui element.
                this.SetProgramToFramework(wallpaper.Handle, PreviewBorder);
                //Fix for wallpaper overlapping window bordere in high dpi screens.
                this.Width += 1;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
            finally
            {
                //Allow closing.
                _isInitialized = true;
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        public void SetWallpaperVolume(int volume)
        {
            wallpaper?.SetVolume(volume);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadingTaskCompletionSource.TrySetResult();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (wallpaper is null)
                return;

            var item = PreviewBorder.GetAbsolutePlacement(true);
            NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
            if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
            {
                NativeMethods.SetWindowPos(wallpaper.Handle, 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
            }
            this.Title = $"{(int)item.Width}x{(int)item.Height} - {model.Title}";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isInitialized)
            {
                e.Cancel = true;
                return;
            }

            if (wallpaper is null)
                return;

            //Detach wallpaper window from this dialogue.
            WindowUtil.TrySetParent(wallpaper.Handle, IntPtr.Zero);
            wallpaper.Close();
            wallpaper.Dispose();
        }
    }
}
