using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.UI.WinUI.Views.Pages.ControlPanel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Lively.UI.WinUI.Services
{
    public class DialogNavigator : IDialogNavigator
    {
        /// <inheritdoc/>
        public event EventHandler<DialogPageType>? ContentPageChanged;

        public object? RootFrame { get; set; }

        /// <inheritdoc/>
        public object? Frame { get; set; }

        public DialogPageType? CurrentPage { get; private set; } = null;

        public void NavigateTo(DialogPageType contentPage, object navArgs = null)
        {
            if (CurrentPage == contentPage)
                return;

            InternalNavigateTo(contentPage, new DrillInNavigationTransitionInfo(), navArgs);
        }

        public void Reload()
        {
            if (CurrentPage == null)
                return;

            InternalNavigateTo(CurrentPage.Value, new EntranceNavigationTransitionInfo());
        }

        private void InternalNavigateTo(DialogPageType contentPage, NavigationTransitionInfo transition, object navArgs = null)
        {
            Type pageType = contentPage switch
            {
                DialogPageType.controlPanelWallpaper => typeof(WallpaperLayoutView),
                DialogPageType.controlPanelScreensaver => typeof(ScreensaverLayoutView),
                DialogPageType.controlPanelCustomise => typeof(WallpaperLayoutCustomiseView),
                _ => throw new NotImplementedException(),
            };

            if (Frame is Frame f)
            {
                f.Navigate(pageType, navArgs, transition);

                CurrentPage = contentPage;
                ContentPageChanged?.Invoke(this, contentPage);
            }
        }
    }
}
