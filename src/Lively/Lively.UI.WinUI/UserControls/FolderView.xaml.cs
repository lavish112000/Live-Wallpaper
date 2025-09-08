using Lively.Models.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Lively.UI.WinUI.UserControls
{
    public sealed partial class FolderView : UserControl
    {
        public string FolderPath
        {
            get { return (string)GetValue(FolderPathProperty); }
            set { SetValue(FolderPathProperty, value); }
        }

        public static readonly DependencyProperty FolderPathProperty =
            DependencyProperty.Register("FolderPath", typeof(string), typeof(FolderView), new PropertyMetadata(null, OnDependencyPropertyChanged));

        public ObservableCollection<ExplorerItem> Data
        {
            get { return (ObservableCollection<ExplorerItem>)GetValue(DataProperty); }
            private set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<ExplorerItem>), typeof(FolderView), new PropertyMetadata(null));

        private static void OnDependencyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var obj = s as FolderView;
            if (e.Property == FolderPathProperty)
            {
                obj.FolderPath = (string)e.NewValue;
                obj.Data = GetDataFromFolder(obj.FolderPath);
            }
        }

        public FolderView()
        {
            this.InitializeComponent();
        }

        private static ObservableCollection<ExplorerItem> GetDataFromFolder(string path, int maxDepth = 2)
        {
            var items = new ObservableCollection<ExplorerItem>();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return items;

            TraverseFolder(path, items, 0, maxDepth);

            return items;
        }

        private static void TraverseFolder(string path, ObservableCollection<ExplorerItem> items, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return;

            try
            {
                // Add directories
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var folderItem = new ExplorerItem
                    {
                        Name = Path.GetFileName(dir),
                        Type = ExplorerItem.ExplorerItemType.Folder
                    };

                    TraverseFolder(dir, folderItem.Children, currentDepth + 1, maxDepth);
                    items.Add(folderItem);
                }

                // Add files
                foreach (var file in Directory.GetFiles(path))
                {
                    items.Add(new ExplorerItem
                    {
                        Name = Path.GetFileName(file),
                        Type = ExplorerItem.ExplorerItemType.File
                    });
                }
            }
            catch (Exception)
            {
                // Skip these files/folder.
            }
        }
    }
}
