using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows;

namespace Lively.ViewModels
{
    public partial class DiagnosticViewModel : ObservableObject
    {
        private readonly IWindowService windowService;

        public DiagnosticViewModel(IWindowService windowService)
        {
            this.windowService = windowService;
            IsGridOverlayVisible = windowService.IsGridOverlayVisible;
        }

        [ObservableProperty]
        private bool isGridOverlayVisible;

        [RelayCommand]
        private void CreateLogFile()
        {
            var saveDlg = new SaveFileDialog
            {
                DefaultExt = ".zip",
                Filter = "Compressed archive (.zip)|*.zip",
                FileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };

            if (saveDlg.ShowDialog() == true)
                SaveLogFile(saveDlg.FileName);
        }

        [RelayCommand]
        private void OpenLogView()
        {
            windowService.ShowLogWindow();
        }

        [RelayCommand]
        private void OpenHelp()
        {
            LinkUtil.OpenBrowser("https://github.com/rocksdanister/lively/wiki/Common-Problems");
        }

        [RelayCommand]
        private void ToggleGridOverlay()
        {
            IsGridOverlayVisible = !IsGridOverlayVisible;
            windowService.ShowGridOverlay(IsGridOverlayVisible);
        }

        private static void SaveLogFile(string fileName)
        {
            try
            {
                LogUtil.ExtractLogFiles(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate log report:\n{ex.Message}", "Lively Wallpaper");
            }
        }
    }
}
