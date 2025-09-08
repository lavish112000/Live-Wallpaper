using Lively.Models;
using Lively.Models.Enums;
using System.Threading.Tasks;

namespace Lively.Common.Factories
{
    public interface IWallpaperLibraryFactory
    {
        LivelyInfoModel GetMetadata(string folderPath);
        LibraryModel CreateFromDirectory(string folderPath);
        LibraryModel CreateFromMetadata(LivelyInfoModel metadata);
        LivelyInfoModel CreateWallpaperPackage(string filePath, string destDirectory, WallpaperType type, string arguments = null);
        Task<LivelyInfoModel> CreateMediaWallpaperPackageAsync(string filePath, string destDirectory, bool copyFileToDest);
        Task ConvertAbsoluteToRelativePathAsync(LivelyInfoModel metadata, string folderPath);
    }
}