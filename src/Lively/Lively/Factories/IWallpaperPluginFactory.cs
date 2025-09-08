using Lively.Common.Services;
using Lively.Core;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Drawing;

namespace Lively.Factories
{
    public interface IWallpaperPluginFactory
    {
        IWallpaper CreateWallpaper(LibraryModel model,
            DisplayMonitor display,
            WallpaperArrangement arrangement,
            bool isWindowed = false);
        IWallpaper CreateDwmThumbnailWallpaper(
            LibraryModel model,
            IntPtr thumbnailSrc,
            Rectangle targetRect,
            DisplayMonitor display);
    }
}