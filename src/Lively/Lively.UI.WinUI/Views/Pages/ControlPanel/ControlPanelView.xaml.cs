using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.UI.Shared.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Lively.UI.WinUI.Views.Pages.ControlPanel
{
    public sealed partial class ControlPanelView : Page
    {
        private readonly IDialogNavigator dialogNavigator;

        public ControlPanelView(ControlPanelViewModel viewModel, IDialogNavigator dialogNavigator)
        {
            this.InitializeComponent();
            this.dialogNavigator = dialogNavigator;
            this.DataContext = viewModel;

            this.dialogNavigator.RootFrame = Root; 
            this.dialogNavigator.Frame = contentFrame;
        }

        private void ControlPanelView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (dialogNavigator.CurrentPage == null)
                dialogNavigator.NavigateTo(DialogPageType.controlPanelWallpaper);
        }
    }
}
