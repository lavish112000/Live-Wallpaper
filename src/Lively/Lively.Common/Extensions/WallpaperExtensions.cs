using Lively.Models.Enums;

namespace Lively.Common.Extensions
{
    public static class WallpaperExtensions
    {
        public static bool IsOnlineWallpaper(this WallpaperType type) => 
            type == WallpaperType.url || type == WallpaperType.videostream;

        public static bool IsWebWallpaper(this WallpaperType type) =>
            type == WallpaperType.web || type == WallpaperType.webaudio || type == WallpaperType.url;

        public static bool IsVideoWallpaper(this WallpaperType type) =>
            type == WallpaperType.video || type == WallpaperType.videostream;

        public static bool IsApplicationWallpaper(this WallpaperType type) =>
            type == WallpaperType.unity || type == WallpaperType.unityaudio || type == WallpaperType.app || type == WallpaperType.godot || type == WallpaperType.bizhawk;

        /// <summary>
        /// Picture, gif and other non dynamic format
        /// </summary>
        public static bool IsMediaWallpaper(this WallpaperType type) => 
            IsVideoWallpaper(type) || type == WallpaperType.gif || type == WallpaperType.picture;

        public static bool IsLocalWebWallpaper(this WallpaperType type) =>
             IsWebWallpaper(type) && !IsOnlineWallpaper(type);

        /// <summary>
        /// Determines if the wallpaper type uses a directory-based structure, meaning it may include multiple files and subdirectories.
        /// </summary>
        public static bool IsDirectoryProject(this WallpaperType type) =>
            IsApplicationWallpaper(type) || IsLocalWebWallpaper(type);

        public static bool IsDeviceInputAllowed(this WallpaperType type)
        {
            return type switch
            {
                WallpaperType.app => true,
                WallpaperType.web => true,
                WallpaperType.webaudio => true,
                WallpaperType.url => true,
                WallpaperType.bizhawk => true,
                WallpaperType.unity => true,
                WallpaperType.godot => true,
                WallpaperType.video => false,
                WallpaperType.gif => false,
                WallpaperType.unityaudio => true,
                WallpaperType.videostream => false,
                WallpaperType.picture => false,
                _ => false,
            };
        }
    }
}
