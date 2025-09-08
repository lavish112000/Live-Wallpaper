using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common.Extensions;
using Lively.Common.Factories;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Core;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.ViewModels
{
    public partial class LibraryPreviewViewModel : ObservableObject
    {
        public event EventHandler<bool> OnWindowCloseRequested;
        private readonly IUserSettingsService userSettings;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;

        // Animated preview parameters, ref: 1. 30c,120s 2. 15c, 90s
        private readonly int previewAnimationDelay = 1000 * 1 / 30; //in milliseconds (1/fps)
        private readonly int previewSaveAnimationDelay = 1000 * 1 / 120;
        private readonly int previewTotalFrames = 60;

        public LibraryPreviewViewModel(IUserSettingsService userSettings, IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.userSettings = userSettings;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
        }

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string author;

        [ObservableProperty]
        private string url;

        [ObservableProperty]
        private bool isUserEditable = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        private bool isProcessing = false;

        [ObservableProperty]
        private double currentProgress;

        public IWallpaper Wallpaper { get; set; }

        public Size CaptureArea { get; set; }

        public Rect CapturePosition { get; set; }

        [RelayCommand]
        private async Task Start()
        {
            try
            {
                IsProcessing = true;
                var fileType = Wallpaper.Model.LivelyInfo.Type;
                var destPath = Wallpaper.Model.LivelyInfoFolderPath;
                var thumbnailPath = Path.Combine(destPath, Path.GetRandomFileName() + ".jpg");
                var previewPath = Path.Combine(destPath, Path.GetRandomFileName() + ".gif");
                // Finalise the changes to disk.
                UpdateWallpaperFiles(Wallpaper.Model,
                    Title,
                    Author,
                    Description,
                    Url,
                    thumbnailPath,
                    previewPath);

                // Start capturing wallpaper preview in-memory.
                var createPreviewTask = CreateThumbnailAndPreview(CaptureArea,
                    CapturePosition,
                    thumbnailPath,
                    previewPath,
                    previewAnimationDelay,
                    previewSaveAnimationDelay,
                    previewTotalFrames,
                    userSettings.Settings.GifCapture && Wallpaper.Category != WallpaperType.picture,
                    new Progress<int>(percent => CurrentProgress = percent - 1));

                // Copy files to destination, ie Absolute path -> Relative path.
                // If editing existing wallpaper, convertion also takes place.
                // Important: Ensure project directory is confirmed by the user before this step,
                // especially to avoid converting critical system/root paths like "C:\index.html".
                var absoluteToRelativePathConvertionTask =
                    wallpaperLibraryFactory.ConvertAbsoluteToRelativePathAsync(Wallpaper.Model.LivelyInfo, destPath);

                await Task.WhenAll(createPreviewTask, absoluteToRelativePathConvertionTask);
            }
            finally 
            {
                IsProcessing = false;
            }
            OnWindowCloseRequested?.Invoke(this, true);
        }

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void Cancel()
        {
            OnWindowCloseRequested?.Invoke(this, false);
        }

        private bool CanCancel() => !IsProcessing;

        public void LoadModel(LivelyInfoModel model)
        {
            Title = model.Title;
            Author = model.Author;
            Description = model.Desc;
            Url = model.Contact;
        }

        private static async Task CreateThumbnailAndPreview(Size area,
            Rect pos,
            string thumbnailFilePath,
            string previewFilePath,
            int previewAnimationDelay,
            int previewSaveAnimationDelay,
            int previewTotalFrames,
            bool isCreatePreview,
            IProgress<int>? progress)
        {
            // Thumbnail art
            CaptureScreen.CopyScreen(
               thumbnailFilePath,
               (int)pos.Left,
               (int)pos.Top,
               (int)area.Width,
               (int)area.Height);

            // Animated preview art
            if (isCreatePreview)
            {
                // Window is locked in position during capture, capture position does not need to be updated.
                await CaptureScreen.CaptureGif(
                       previewFilePath,
                       (int)pos.Left,
                       (int)pos.Top,
                       (int)pos.Width,
                       (int)pos.Height,
                       previewAnimationDelay,
                       previewSaveAnimationDelay,
                       previewTotalFrames,
                       progress);
            }
        }

        private static void UpdateWallpaperFiles(
            LibraryModel model,
            string title,
            string author,
            string description,
            string url,
            string thumbnailPath,
            string previewPath)
        {
            try
            {
                // Delete previous thumbnail/preview if any.
                if (File.Exists(model.ThumbnailPath))
                    File.Delete(model.ThumbnailPath);
                if (File.Exists(model.PreviewClipPath))
                    File.Delete(model.PreviewClipPath);
            }
            catch { /* Nothing to do */ }

            // Update metadata.
            model.LivelyInfo.Title = title;
            model.LivelyInfo.Author = author;
            model.LivelyInfo.Desc = description;
            model.LivelyInfo.Contact = url;
            model.LivelyInfo.Thumbnail = model.LivelyInfo.IsAbsolutePath ? thumbnailPath : Path.GetFileName(thumbnailPath);
            model.LivelyInfo.Preview = model.LivelyInfo.IsAbsolutePath ? previewPath : Path.GetFileName(previewPath);
            // Update metadata file.
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(model.LivelyInfoFolderPath, "LivelyInfo.json"), model.LivelyInfo);
        }
    }
}
