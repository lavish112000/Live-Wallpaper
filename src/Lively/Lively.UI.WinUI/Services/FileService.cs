using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Files;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.UI.WinUI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.WinUI.Services
{
    //References:
    //https://github.com/microsoft/WindowsAppSDK/issues/2504
    //https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/w5tyztk9(v=vs.100)
    //https://gist.github.com/gotmachine/4ffaf7837f9fbb0ab4a648979ee40609
    //https://learn.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamea
    public class FileService : IFileService
    {
        private readonly IResourceService i18n;

        public FileService(IResourceService i18n)
        {
            this.i18n = i18n;
        }

        public async Task<IReadOnlyList<string>> PickFileAsync(IEnumerable<(string label, string[] extensions)> filters, bool multipleFile = false)
        {
            if (UAC.IsElevated)
            {
                var filterString = GetFilterNative(filters);

                return multipleFile ?
                    FileDialogNative.PickMultipleFiles(filterString) :
                    FileDialogNative.PickSingleFile(filterString) is string file ? [file] : [];
            }
            else
            {
                var filterArray = GetFilterUwp(filters);

                return multipleFile ?
                   await PickMultipleFileUwp(filterArray) :
                   await PickSingleFileUwp(filterArray) is string file ? [file] : [];
            }
        }

        public async Task<IReadOnlyList<string>> PickFileAsync(WallpaperType type, bool multipleFile = false)
        {
            var filters = GetFilter(type);
            return await PickFileAsync([filters], multipleFile);
        }

        public async Task<IReadOnlyList<string>> PickWallpaperFile(bool multipleFile = false)
        {
            var filters = GetWallpaperFilters(true);
            filters.Add(("Lively Wallpaper", [".zip"]));

            return await PickFileAsync(filters, multipleFile);
        }

        public async Task<string> PickSaveFileAsync(string suggestedFileName, IEnumerable<(string label, string[] extensions)> fileTypeChoices)
        {
            if (UAC.IsElevated)
            {
                string filter = GetFilterNative(fileTypeChoices);
                string defaultExt = fileTypeChoices.FirstOrDefault().extensions?.FirstOrDefault();

                return FileDialogNative.PickSaveFile(filter, suggestedFileName, defaultExt);
            }
            else
            {
                var filePicker = new FileSavePicker();
                filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
                foreach (var item in fileTypeChoices)
                    filePicker.FileTypeChoices.Add(item.label, item.extensions);
                filePicker.SuggestedFileName = suggestedFileName;

                var file = await filePicker.PickSaveFileAsync();
                return file?.Path;
            }
        }

        public async Task<string> PickFolderAsync(string[] filters)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filters)
            {
                folderPicker.FileTypeFilter.Add(item);
            }
            return (await folderPicker.PickSingleFolderAsync())?.Path;
        }

        public async Task OpenFolderAsync(string path)
        {
            if (!PackageUtil.IsRunningAsPackaged)
            {
                FileUtil.OpenFolder(path);
            }
            else
            {
                try
                {
                    var packagePath = PackageUtil.ValidateAndResolvePath(path);
                    var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(packagePath));
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                }
                catch { }
            }
        }

        private static async Task<string> PickSingleFileUwp(string[] filters)
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            foreach (var item in filters)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            return (await filePicker.PickSingleFileAsync())?.Path;
        }

        private static async Task<IReadOnlyList<string>> PickMultipleFileUwp(string[] filters)
        {
            var filePicker = new FileOpenPicker();
            foreach (var item in filters)
            {
                filePicker.FileTypeFilter.Add(item);
            }
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            var files = await filePicker.PickMultipleFilesAsync();
            return files.Any() ? files.Select(x => x.Path).ToList() : new List<string>();
        }

        private List<(string label, string[] extensions)> GetWallpaperFilters(bool includeAllFiles = false)
        {
            var filters = new List<(string label, string[] extensions)>();
            if (includeAllFiles)
                filters.Add((i18n.GetString("TextAllFiles"), ["*"]));

            foreach (var format in FileTypes.SupportedFormats)
                filters.Add(GetFilter(format.Type));

            return filters;
        }

        private (string label, string[] extensions) GetFilter(WallpaperType wallpaperType)
        {
            var format = FileTypes.SupportedFormats.First(x => x.Type == wallpaperType);
            var label = i18n.GetString(format.Type);
            return (label, format.Extentions);
        }

        private static string[] GetFilterUwp(IEnumerable<(string label, string[] extensions)> filters) => filters
            .SelectMany(f => f.extensions)
            .ToArray();

        private static string GetFilterNative(IEnumerable<(string label, string[] extensions)> filters)
        {
            // Format: "label1\0*.ext1;*.ext2\0Label2\0*.ext3\0\0"
            var sb = new StringBuilder();
            foreach (var (label, extList) in filters)
            {
                // Uwp -> native.
                var pattern = string.Join(";", extList.Select(ext => ext == "*" ? "*.*" : $"*{ext}"));
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                sb.Append(label).Append('\0');
                sb.Append(pattern).Append('\0');
            }
            sb.Append('\0');

            return sb.ToString();
        }
    }
}
