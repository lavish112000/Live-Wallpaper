using System.Collections.ObjectModel;

namespace Lively.Models.UserControls;

// https://github.com/microsoft/WinUI-Gallery
// MIT License
public class ExplorerItem
{
    public enum ExplorerItemType
    {
        Folder,
        File,
    }

    public string Name { get; set; }
    public ExplorerItemType Type { get; set; }
    public ObservableCollection<ExplorerItem> Children { get; set; } = [];
}
