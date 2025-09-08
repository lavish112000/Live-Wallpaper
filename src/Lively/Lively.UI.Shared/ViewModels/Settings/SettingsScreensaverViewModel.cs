using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Helpers;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using System.Diagnostics;

namespace Lively.UI.Shared.ViewModels
{
    public partial class SettingsScreensaverViewModel : ObservableObject
    {
        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;

        public SettingsScreensaverViewModel(IUserSettingsClient userSettings, IDispatcherService dispatcher)
        {
            this.userSettings = userSettings;
            this.dispatcher = dispatcher;

            IsFadeIn = userSettings.Settings.ScreensaverFadeIn;
            IsLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
            Volume = userSettings.Settings.ScreensaverGlobalVolume;
            IsScreensaverPluginNotify = !ScreensaverUtil.IsScreensaverSelected("Lively");
        }

        [ObservableProperty]
        private bool isScreensaverPluginNotify;

        private bool _isLockOnResume;
        public bool IsLockOnResume
        {
            get => _isLockOnResume;
            set
            {
                if (userSettings.Settings.ScreensaverLockOnResume != value)
                {
                    userSettings.Settings.ScreensaverLockOnResume = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isLockOnResume, value);
            }
        }

        private bool _isFadeIn;
        public bool IsFadeIn
        {
            get => _isFadeIn;
            set
            {
                if (userSettings.Settings.ScreensaverFadeIn != value)
                {
                    userSettings.Settings.ScreensaverFadeIn = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isFadeIn, value);
            }
        }

        private int _volume;
        public int Volume
        {
            get => _volume;
            set
            {
                if (userSettings.Settings.ScreensaverGlobalVolume != value)
                {
                    userSettings.Settings.ScreensaverGlobalVolume = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _volume, value);
            }
        }

        [RelayCommand]
        private void OpenWindowsSettings()
        {
            try
            {
                // Ref: https://help.ivanti.com/res/help/en_us/iwc/2021/help/Content/20030.htm
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "rundll32.exe",
                    Arguments = "shell32.dll,Control_RunDLL desk.cpl,,1",
                    UseShellExecute = true
                });
            }
            catch { /* Nothing to do */ }
        }

        private void UpdateSettingsConfigFile()
        {
            dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }
    }
}
