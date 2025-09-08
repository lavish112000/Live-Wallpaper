using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Models.Enums;

namespace Lively.Models.UserControls
{
    public partial class DialogNavigationItem : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string glyph;

        [ObservableProperty]
        private bool isVisible = true;

        [ObservableProperty]
        private DialogPageType pageType;
    }
}
