using Lively.Common.Services;
using Lively.Models.Enums;
using System;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Markup;

namespace Lively.Services
{
    public class ResourceService : IResourceService
    {
        public event EventHandler<string> CultureChanged;

        private readonly ResourceManager resourceManager;

        public ResourceService()
        {
            resourceManager = Properties.Resources.ResourceManager;
        }

        public void SetCulture(string name)
        {
            CultureInfo culture;
            try
            {
                // CultureInfo.CurrentUICulture is no longer reliable since we are changing DefaultThreadCurrentUICulture, so we use win32 to retrive system culture.
                culture = string.IsNullOrEmpty(name) ?
                    GetSystemDefaultUICulture() : new CultureInfo(name);
            }
            catch
            {
                // Invalid culture, just keep using system default.
                return;
            }

            if (CultureInfo.DefaultThreadCurrentCulture?.Name == culture.Name)
                return;

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            // Force UI refresh
            foreach (Window window in Application.Current.Windows)
                window.Language = XmlLanguage.GetLanguage(culture.Name);

            CultureChanged?.Invoke(this, culture.Name);
        }

        public void SetSystemDefaultCulture()
        {
            SetCulture(string.Empty);
        }

        public string GetString(string resource)
        {
            // Compatibility with UWP .resw shared classes.
            // Compatibility with WPF Xaml.
            var formattedResource = resource.Replace("/", ".").Replace("_", ".");
            var culture = CultureInfo.DefaultThreadCurrentCulture;
            return culture != null ? 
                resourceManager.GetString(formattedResource, culture) : resourceManager.GetString(formattedResource);
        }

        public string GetString(WallpaperType type)
        {
            return type switch
            {
                WallpaperType.app => resourceManager.GetString("TextApplication"),
                WallpaperType.unity => "Unity",
                WallpaperType.godot => "Godot",
                WallpaperType.unityaudio => "Unity",
                WallpaperType.bizhawk => "Bizhawk",
                WallpaperType.web => resourceManager.GetString("Website/Header"),
                WallpaperType.webaudio => resourceManager.GetString("AudioGroup/Header"),
                WallpaperType.url => resourceManager.GetString("Website/Header"),
                WallpaperType.video => resourceManager.GetString("TextVideo"),
                WallpaperType.gif => "Gif",
                WallpaperType.videostream => resourceManager.GetString("TextWebStream"),
                WallpaperType.picture => resourceManager.GetString("TextPicture"),
                //WallpaperType.heic => "HEIC",
                (WallpaperType)(100) => "Lively Wallpaper",
                _ => resourceManager.GetString("TextError"),
            };
        }

        // Ref: https://pinvoke.net/default.aspx/kernel32.GetUserPreferredUILanguages
        private static CultureInfo GetSystemDefaultUICulture()
        {
            StringBuilder languagesBuffer = new();
            uint languagesCount, languagesBufferSize = 0;

            if (GetUserPreferredUILanguages(
                MUI_LANGUAGE_NAME,
                out languagesCount,
                null,
                ref languagesBufferSize))
            {
                languagesBuffer.EnsureCapacity((int)languagesBufferSize);
                if (GetUserPreferredUILanguages(
                    MUI_LANGUAGE_NAME,
                    out languagesCount,
                    languagesBuffer,
                    ref languagesBufferSize))
                {
                    string[] languages = languagesBuffer.ToString().Split(['\0'], StringSplitOptions.RemoveEmptyEntries);
                    return new CultureInfo(languages[0]);
                }
            }

            return CultureInfo.InvariantCulture;
        }

        const uint MUI_LANGUAGE_NAME = 0x8; // Use ISO language (culture) name convention

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetUserPreferredUILanguages(
            uint dwFlags,
            out uint pulNumLanguages,
            StringBuilder pwszLanguagesBuffer,
            ref uint pcchLanguagesBuffer);
    }
}
