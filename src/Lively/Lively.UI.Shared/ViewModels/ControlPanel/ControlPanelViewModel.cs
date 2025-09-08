using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.Models.UserControls;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ControlPanelViewModel : ObservableObject
    {
        public WallpaperLayoutViewModel WallpaperVm { get; }
        public ScreensaverLayoutViewModel ScreensaverVm { get; }
        private readonly IDialogNavigator dialogNavigator;

        public ControlPanelViewModel(WallpaperLayoutViewModel wallpaperVm,
            ScreensaverLayoutViewModel screensaverVm,
            IDialogNavigator dialogNavigator,
            IResourceService i18n)
        {
            this.WallpaperVm = wallpaperVm;
            this.ScreensaverVm = screensaverVm;
            this.dialogNavigator = dialogNavigator;

            MenuItems = [
                new() { Name = i18n.GetString("TitleWallpaper/Content"), PageType = DialogPageType.controlPanelWallpaper},
                new() { Name = i18n.GetString("TitleScreensaver/Content"), PageType = DialogPageType.controlPanelScreensaver },
                new() { Name = i18n.GetString("TitleCustomise/Content"), PageType = DialogPageType.controlPanelCustomise, IsVisible = false }
            ];
            // SelectedMenuItem set in View Loaded event.

            this.WallpaperVm.PropertyChanged += WallpaperVm_PropertyChanged;
            this.ScreensaverVm.PropertyChanged += ScreensaverVm_PropertyChanged;
            this.dialogNavigator.ContentPageChanged += DialogNavigator_ContentPageChanged;
        }

        [ObservableProperty]
        private DialogPageType? currentPage;

        [ObservableProperty]
        private bool isHideDialog;

        [ObservableProperty]
        private bool isShowScreensaverSettings;

        [ObservableProperty]
        private ObservableCollection<DialogNavigationItem> menuItems;

        private DialogNavigationItem selectedMenuItem;
        public DialogNavigationItem SelectedMenuItem
        {
            get => selectedMenuItem;
            set
            {
                SetProperty(ref selectedMenuItem, value);

                if (selectedMenuItem != null)
                    dialogNavigator.NavigateTo(selectedMenuItem.PageType);
            }
        }

        [RelayCommand]
        private void NavigateBackWallpaper()
        {
            dialogNavigator.NavigateTo(DialogPageType.controlPanelWallpaper);
        }

        private void ScreensaverVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScreensaverVm.IsHideDialog))
                IsHideDialog = ScreensaverVm.IsHideDialog;
            else if (e.PropertyName == nameof(ScreensaverVm.IsShowSettings))
                IsShowScreensaverSettings = ScreensaverVm.IsShowSettings;
        }

        private void WallpaperVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(WallpaperVm.SelectedWallpaperLayout))
                ScreensaverVm.SelectedScreensaverArrangement = (ScreensaverType)ScreensaverVm.SelectedScreensaverTypeIndex != ScreensaverType.wallpaper ? 
                    ScreensaverVm.SelectedScreensaverArrangement : WallpaperVm.SelectedWallpaperLayout;
        }

        private void DialogNavigator_ContentPageChanged(object sender, DialogPageType pageType)
        {
            // Update visibility
            MenuItems.First(x => x.PageType == DialogPageType.controlPanelCustomise).IsVisible = pageType == DialogPageType.controlPanelCustomise;

            // Update selection UI if navigation is called by code.
            if (SelectedMenuItem == null || SelectedMenuItem.PageType != pageType)
                SelectedMenuItem = MenuItems.FirstOrDefault(x => x.PageType == pageType);

            // To save customisation to disk.
            if (CurrentPage is not null && CurrentPage == DialogPageType.controlPanelCustomise)
                WallpaperVm.CustomiseWallpaperPageOnClosed();

            CurrentPage = pageType;
        }

        public void OnWindowClosing(object sender, object e)
        {
            WallpaperVm?.OnWindowClosing();
            ScreensaverVm?.OnWindowClosing();

            this.ScreensaverVm.PropertyChanged -= ScreensaverVm_PropertyChanged;
        }
    }
}
