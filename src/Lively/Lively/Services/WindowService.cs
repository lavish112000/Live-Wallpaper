using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Extensions;
using Lively.Views;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lively.Services
{
    public class WindowService : IWindowService
    {
        public bool IsGridOverlayVisible => isGridOverlayVisible;

        private readonly IDisplayManager displayManager;
        private readonly IRunnerService runner;
        private readonly List<WindowCoverageDebugOverlay> gridOverlays = [];
        private bool isGridOverlayVisible;
        private DebugLog debugLogWindow;

        public WindowService(IRunnerService runner, IDisplayManager displayManager)
        {
            this.runner = runner;
            this.displayManager = displayManager;
        }

        public void ShowLogWindow()
        {
            if (debugLogWindow != null)
                return;

            debugLogWindow = new DebugLog();
            debugLogWindow.Closed += (s, e) => debugLogWindow = null;
            debugLogWindow.Show();
        }

        public void ShowGridOverlay(bool isVisible)
        {
            if (isVisible)
                ShowGridOverlayInternal();
            else
                CloseGridOverlayInternal();
        }


        public async Task<bool> ShowWallpaperDialogWindowAsync(object wallpaper)
        {
            bool? success = false;
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                var previewWindow = new LibraryPreview(wallpaper as IWallpaper)
                {
                    Topmost = true,
                    ShowActivated = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                previewWindow.Loaded += (s, e) =>
                {
                    if (runner.IsVisibleUI)
                        previewWindow.CenterToWindow(runner.HwndUI);
                };
                success = previewWindow.ShowDialog();
            }));
            return success ?? false;
        }

        private void ShowGridOverlayInternal()
        {
            if (isGridOverlayVisible)
                return;

            isGridOverlayVisible = true;
            foreach (var display in displayManager.DisplayMonitors)
            {
                var gridOverlay = new WindowCoverageDebugOverlay(display);
                gridOverlay.Show();
                gridOverlays.Add(gridOverlay);
            }
        }

        private void CloseGridOverlayInternal()
        {
            if (!isGridOverlayVisible)
                return;

            isGridOverlayVisible = false;
            foreach (var gridOverlay in gridOverlays)
                gridOverlay.Close();

            gridOverlays.Clear();
        }
    }
}
