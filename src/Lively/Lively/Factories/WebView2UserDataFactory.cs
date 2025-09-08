using Lively.Common;
using Lively.Common.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.IO;

namespace Lively.Factories
{
    public class WebView2UserDataFactory : IWebView2UserDataFactory
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string GetUserDataFolder(WallpaperArrangement arrangement, DisplayMonitor display)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(Constants.PlayerPartialPaths.WebView2Path);
            var baseDir = Constants.CommonPaths.TempWebView2Dir;
            // If same UserData folder with same parameters is used WebView2 shares process.
            // If same UserData with different parameter then WebView2 initialization fail.
            // Ref: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder?tabs=win32
            // Ref: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/process-model?tabs=csharp
            var subFolder = arrangement switch
            {
                WallpaperArrangement.per => Path.Combine("PerScreen", $"Monitor_{display.Index}"),
                WallpaperArrangement.span => "Span",
                WallpaperArrangement.duplicate => "Duplicate",
                _ => Path.Combine("PerScreen", $"Monitor_{display.Index}"),
            };

            if (PackageUtil.IsRunningAsPackaged)
            {
                try
                {
                    // WebView2 runs outside the packaged sandbox, so we must redirect paths to the packaged LocalCache if it exists.
                    // This workaround addresses a known issue on Windows 10 22H2, where WebView2 does not automatically pick up the redirected path.
                    baseDir = PackageUtil.ValidateAndResolvePath(Constants.CommonPaths.TempWebView2Dir);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return Path.Combine(baseDir, assemblyName, subFolder);
        }

        /// <inheritdoc/>
        public string GetTempUserDataFolder()
        {
            var baseDir = Constants.CommonPaths.CefRootCacheDir;

            if (PackageUtil.IsRunningAsPackaged)
            {
                try
                {
                    baseDir = PackageUtil.ValidateAndResolvePath(Constants.CommonPaths.CefRootCacheDir);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return Path.Combine(baseDir, Path.GetRandomFileName());
        }
    }
}
