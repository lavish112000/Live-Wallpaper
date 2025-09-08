using Lively.Common.Extensions;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Models;
using Lively.Models.Enums;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Lively.Common.Factories
{
    public class WallpaperLibraryFactory : IWallpaperLibraryFactory
    {
        public LivelyInfoModel GetMetadata(string folderPath)
        {
            if (!File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
                throw new FileNotFoundException("LivelyInfo.json not found");

            return JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json")) ?? throw new FileNotFoundException("Corrupted wallpaper metadata");
        }

        public LibraryModel CreateFromDirectory(string folderPath)
        {
            var metadata = GetMetadata(folderPath);
            var result = new LibraryModel
            {
                LivelyInfo = metadata,
                LivelyInfoFolderPath = folderPath,
                LivelyInfoLocalizationPath = Path.Combine(folderPath, "LivelyInfo.loc.json"),
                LivelyPropertyLocalizationPath = Path.Combine(folderPath, "LivelyProperties.loc.json"),
                IsSubscribed = !string.IsNullOrEmpty(metadata.Id),
                Title = metadata.Title,
                Desc = metadata.Desc,
                Author = metadata.Author
            };
            if (metadata.IsAbsolutePath)
            {
                //Full filepath is stored in Livelyinfo.json metadata file.
                result.FilePath = metadata.FileName;
                //This is to keep backward compatibility with older wallpaper files.
                //When I originally made the property all the paths where made absolute, not just wallpaper path.
                //But previewgif and thumb are always inside the temporary lively created folder.
                result.PreviewClipPath = TryPathCombine(folderPath, Path.GetFileName(metadata.Preview));
                result.ThumbnailPath = TryPathCombine(folderPath, Path.GetFileName(metadata.Thumbnail));

                try
                {
                    result.LivelyPropertyPath = Path.Combine(Directory.GetParent(metadata.FileName).FullName, "LivelyProperties.json");
                }
                catch
                {
                    result.LivelyPropertyPath = null;
                }
            }
            else
            {
                //Only relative path is stored, this will be inside the appdata folder
                if (metadata.Type.IsOnlineWallpaper())
                    result.FilePath = metadata.FileName;
                else
                {
                    result.FilePath = TryPathCombine(folderPath, metadata.FileName);
                    result.LivelyPropertyPath = TryPathCombine(folderPath, "LivelyProperties.json");
                }
                result.PreviewClipPath = TryPathCombine(folderPath, metadata.Preview);
                result.ThumbnailPath = TryPathCombine(folderPath, metadata.Thumbnail);
            }

            //Verify
            result.ThumbnailPath = File.Exists(result.ThumbnailPath) ? result.ThumbnailPath : null;
            result.PreviewClipPath = File.Exists(result.PreviewClipPath) ? result.PreviewClipPath : null;
            //Use preview if available
            result.ImagePath = result.PreviewClipPath ?? result.ThumbnailPath;
            //Default video player property, otherwise verify if wallpaper is customisable
            if (metadata.Type.IsMediaWallpaper())
            {
                result.LivelyPropertyPath = File.Exists(result.LivelyPropertyPath) ?
                    result.LivelyPropertyPath : Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.json");

                result.LivelyPropertyLocalizationPath = File.Exists(result.LivelyPropertyLocalizationPath) ?
                    result.LivelyPropertyLocalizationPath : Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.loc.json");
            }
            else
                result.LivelyPropertyPath = File.Exists(result.LivelyPropertyPath) ? result.LivelyPropertyPath : null;

            return result;
        }

        private static string TryPathCombine(string path1, string path2)
        {
            try
            {
                return Path.Combine(path1, path2);
            }
            catch
            {
                return null;
            }
        }

        public LibraryModel CreateFromMetadata(LivelyInfoModel metadata)
        {
            return new LibraryModel()
            {
                LivelyInfo = metadata,
                Title = metadata.Title,
                Desc = metadata.Desc,
                Author = metadata.Author,
                ImagePath = File.Exists(metadata.Preview) ? metadata.Preview : metadata.Thumbnail,
            };
        }

        public LivelyInfoModel CreateWallpaperPackage(string filePath, string destDirectory, WallpaperType type, string arguments = null)
        {
            var contact = type.IsOnlineWallpaper() ? filePath : string.Empty;
            var title = type.IsOnlineWallpaper() ? LinkUtil.GetLastSegmentUrl(filePath) : Path.GetFileNameWithoutExtension(filePath);
            var metadata = new LivelyInfoModel()
            {
                Title = title,
                Type = type,
                IsAbsolutePath = true,
                FileName = filePath,
                Contact = contact,
                Preview = string.Empty,
                Thumbnail = string.Empty,
                Arguments = arguments,
            };

            Directory.CreateDirectory(destDirectory);
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(destDirectory, "LivelyInfo.json"), metadata);
            return metadata;
        }

        public async Task<LivelyInfoModel> CreateMediaWallpaperPackageAsync(string filePath, string destDirectory, bool copyFileToDest)
        {
            var fileType = FileTypes.GetFileType(filePath);
            if (!fileType.IsMediaWallpaper() || fileType.IsOnlineWallpaper())
                return null;

            var title = Path.GetFileNameWithoutExtension(filePath);
            var thumbnailPath = Path.Combine(destDirectory, Path.GetRandomFileName() + ".jpg");
            var metadata = new LivelyInfoModel()
            {
                Title = title,
                Type = fileType,
                IsAbsolutePath = true,
                FileName = filePath,
                Contact = string.Empty,
                Preview = string.Empty,
                Thumbnail = thumbnailPath,
                Arguments = string.Empty,
            };

            Directory.CreateDirectory(destDirectory);
            // Create thumbnail, 512x512 quality is not guaranteed depending on file format and system codecs.
            using var thumbnail = ThumbnailUtil.GetThumbnail(filePath, 512, 512, ThumbnailUtil.ThumbnailOptions.None);
            thumbnail.Save(thumbnailPath, ImageFormat.Jpeg);
            // Update metadata file.
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(destDirectory, "LivelyInfo.json"), metadata);

            if (copyFileToDest)
                await ConvertAbsoluteToRelativePathAsync(metadata, destDirectory);

            return metadata;
        }

        public async Task ConvertAbsoluteToRelativePathAsync(LivelyInfoModel metadata, string folderPath)
        {
            if (!metadata.IsAbsolutePath)
                return;

            switch (metadata.Type)
            {
                case WallpaperType.video:
                case WallpaperType.gif:
                case WallpaperType.picture:
                    {
                        // Copy media file to destination.
                        var sourcePath = metadata.FileName;
                        var destinationPath = Path.Combine(folderPath, Path.GetFileName(sourcePath));
                        await Task.Run(() => File.Copy(sourcePath, destinationPath, false));
                        // Change fullpath to relative.
                        metadata.FileName = Path.GetFileName(sourcePath);
                    }
                    break;
                case WallpaperType.unityaudio:
                case WallpaperType.app:
                case WallpaperType.web:
                case WallpaperType.webaudio:
                case WallpaperType.bizhawk:
                case WallpaperType.unity:
                case WallpaperType.godot:
                    {
                        // Copy the entire directory containing the file.
                        var sourceDir = Path.GetDirectoryName(metadata.FileName);
                        await Task.Run(() => FileUtil.DirectoryCopy(sourceDir, folderPath, true));
                        // Change fullpath to relative.
                        metadata.FileName = Path.GetFileName(metadata.FileName);
                    }
                    break;
                case WallpaperType.url:
                case WallpaperType.videostream:
                    {
                        // Nothing to do
                    }
                    break;
            }

            // Change fullpath to relative.
            metadata.Thumbnail = File.Exists(metadata.Thumbnail) ? Path.GetFileName(metadata.Thumbnail) : null;
            metadata.Preview = File.Exists(metadata.Preview) ? Path.GetFileName(metadata.Preview) : null;
            metadata.IsAbsolutePath = false;

            // Update wallpaper metadata file.
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(folderPath, "LivelyInfo.json"), metadata);
        }
    }
}