using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Factories;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Services;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lively
{
    public class AppInitializer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly IUserSettingsService userSettings;

        public AppInitializer(IUserSettingsService userSettings, IWallpaperLibraryFactory wallpaperLibraryFactory)
        {
            this.userSettings = userSettings;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
        }

        public void Run()
        {
            CleanTempFiles();
            CreateRequiredDirectories();
            SetupWallpaperDirectories();
            HandleFirstRunOrUpdate(true);
        }

        private void CleanTempFiles()
        {
            try
            {
                // Clear temp files from previous run if any..
                FileUtil.EmptyDirectory(Constants.CommonPaths.TempDir);
                FileUtil.EmptyDirectory(Constants.CommonPaths.ThemeCacheDir);
                FileUtil.EmptyDirectory(Constants.CommonPaths.CefRootCacheDir);
                FileUtil.EmptyDirectory(Constants.CommonPaths.ScreenshotDir);
            }
            catch { /* Nothing to do */ }
        }

        private void CreateRequiredDirectories()
        {
            // If these fail, throw the exception and abort.
            Directory.CreateDirectory(Constants.CommonPaths.AppDataDir);
            Directory.CreateDirectory(Constants.CommonPaths.LogDir);
            Directory.CreateDirectory(Constants.CommonPaths.ThemeDir);
            Directory.CreateDirectory(Constants.CommonPaths.TempDir);
            Directory.CreateDirectory(Constants.CommonPaths.TempCefDir);
            Directory.CreateDirectory(Constants.CommonPaths.TempVideoDir);
            Directory.CreateDirectory(Constants.CommonPaths.ThemeCacheDir);
            Directory.CreateDirectory(Constants.CommonPaths.TempWebView2Dir);
            Directory.CreateDirectory(Constants.CommonPaths.ScreenshotDir);
            Directory.CreateDirectory(Constants.CommonPaths.CefRootCacheDir);
        }

        private void SetupWallpaperDirectories()
        {
            try
            {
                CreateWallpaperDir(userSettings.Settings.WallpaperDir);
            }
            catch (Exception ex)
            {
                Logger.Error($"Wallpaper directory setup failed: {ex.Message}, falling back to default.");
                userSettings.Settings.WallpaperDir = Path.Combine(Constants.CommonPaths.AppDataDir, "Library");
                CreateWallpaperDir(userSettings.Settings.WallpaperDir);
                userSettings.Save<SettingsModel>();
            }
        }

        private void HandleFirstRunOrUpdate(bool showSplash)
        {
            // Install any new asset collection if present, do this before restoring wallpaper incase wallpaper is updated.
            if (userSettings.Settings.IsUpdated || userSettings.Settings.IsFirstRun)
            {
                SplashWindow spl = null;
                if (showSplash)
                {
                    spl = new SplashWindow(0, 500);
                    spl.Show();
                }

                InstallWallpaperBundles();
                SetupWallpaperDefaults();
                MigrateFromOlderVersions();

                spl?.Close();
            }
        }

        private void InstallWallpaperBundles()
        {
            // Install default wallpapers or updates.
            var maxWallpaper = ZipExtract.ExtractAssetBundle(userSettings.Settings.WallpaperBundleVersion,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle", "wallpapers"),
                Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir));
            var maxTheme = ZipExtract.ExtractAssetBundle(userSettings.Settings.ThemeBundleVersion,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle", "themes"),
                Path.Combine(Constants.CommonPaths.ThemeDir));
            if (maxTheme != userSettings.Settings.ThemeBundleVersion || maxWallpaper != userSettings.Settings.WallpaperBundleVersion)
            {
                userSettings.Settings.WallpaperBundleVersion = maxWallpaper;
                userSettings.Settings.ThemeBundleVersion = maxTheme;
                userSettings.Save<SettingsModel>();
            }
        }

        private void SetupWallpaperDefaults()
        {
            try
            {
                // Default media wallpaper LivelyProperty configuration.
                var assetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Plugins", "Mpv");
                var livelyPropertyPath = Path.Combine(assetDir, "LivelyProperties.json");
                var livelyPropertyLocPath = Path.Combine(assetDir, "LivelyProperties.loc.json");

                if (File.Exists(livelyPropertyPath))
                    File.Copy(livelyPropertyPath, Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.json"), true);
                if (File.Exists(livelyPropertyLocPath))
                    File.Copy(livelyPropertyLocPath, Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.loc.json"), true);
            }
            catch { /* Nothing to do */ }
        }

        private void MigrateFromOlderVersions()
        {
            if (!userSettings.Settings.IsUpdated || string.IsNullOrWhiteSpace(userSettings.Settings.AppPreviousVersion))
                return;

            var fromVersion = new Version(userSettings.Settings.AppPreviousVersion);

            if (fromVersion < new Version(2, 1, 0, 0))
            {
                // Mpv property file changed in v2.1, delete user data.
                var dir = new List<string>();
                string[] folderPaths = {
                        Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                        Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
                    };
                for (int i = 0; i < folderPaths.Count(); i++)
                {
                    try
                    {
                        dir.AddRange(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                    }
                    catch { /* TODO */ }
                }

                for (int i = 0; i < dir.Count; i++)
                {
                    try
                    {
                        var metadata = wallpaperLibraryFactory.GetMetadata(dir[i]);
                        if (metadata.Type.IsMediaWallpaper())
                        {
                            var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
                            var wallpaperDataFolder = Path.Combine(dataFolder, new DirectoryInfo(dir[i]).Name);
                            if (Directory.Exists(wallpaperDataFolder))
                                Directory.Delete(wallpaperDataFolder, true);
                        }
                    }
                    catch { }
                }
            }

            if (fromVersion < new Version(2, 2, 0, 0))
            {
                // Default, same as system.
                userSettings.Settings.Language = string.Empty;
                // Reset webplayer settings.
                userSettings.Settings.WebBrowser = Constants.AppDefaults.WebBrowser;
                userSettings.Settings.CefDiskCache = false;
                userSettings.Settings.WebDebugPort = string.Empty;
                // New pause algorithm, lets reset.
                userSettings.Settings.ProcessMonitorAlgorithm = ProcessMonitorAlgorithm.grid;
                userSettings.Settings.AppFocusPause = AppRules.ignore;
                userSettings.Settings.AppFullscreenPause = AppRules.pause;
                userSettings.Settings.DisplayPauseSettings = DisplayPause.perdisplay;
                userSettings.AppRules.Clear();
                // Apply changes
                userSettings.Save<SettingsModel>();
                userSettings.Save<List<ApplicationRulesModel>>();
            }
        }

        private static void CreateWallpaperDir(string baseDirectory)
        {
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir));
        }
    }
}
