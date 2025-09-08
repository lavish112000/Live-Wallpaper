using Lively.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Prompts the user to pick one or more files with optional filtering by file type.
        /// </summary>
        /// <param name="filters">
        /// A collection of file type filters. Each filter is a tuple containing:
        /// - <c>label</c>: A string describing the file type.
        /// - <c>extensions</c>: An array of file extensions.
        /// 
        /// Example: <c>[("Pictures", new[] { ".jpeg", ".jpg", ".png", ".gif" })]</c>
        /// </param>
        /// <param name="multipleFile">Whether multiple files can be selected. Defaults to <c>false</c>.</param>
        public Task<IReadOnlyList<string>> PickFileAsync(IEnumerable<(string label, string[] extensions)> filters, bool multipleFile = false);

        public Task<IReadOnlyList<string>> PickFileAsync(WallpaperType type, bool multipleFile = false);

        public Task<IReadOnlyList<string>> PickWallpaperFile(bool multipleFile = false);

        public Task<string> PickSaveFileAsync(string suggestedFileName, IEnumerable<(string label, string[] extensions)> fileTypeChoices);

        public Task<string> PickFolderAsync(string[] filters);

        public Task OpenFolderAsync(string path);
    }
}
