using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Factories
{
    public interface IWebView2UserDataFactory
    {
        /// <summary>
        /// Returns the user data folder path for a WebView2 instance based on wallpaper arrangement and monitor.
        /// <list type="bullet">
        /// <item><description>Per-screen: <c>PerScreen/Monitor_{index}</c></description></item>
        /// <item><description>Span: <c>Span</c></description></item>
        /// <item><description>Duplicate: <c>Duplicate</c></description></item>
        /// </list>
        /// When running as a packaged app, attempts to resolve redirected LocalCache paths
        /// </summary>
        string GetUserDataFolder(WallpaperArrangement arrangement, DisplayMonitor display);

        /// <summary>
        /// Returns a temporary user data folder path for WebView2.
        /// <para>
        /// WebView2 does not currently support an in-memory cache mode
        /// (see: https://github.com/MicrosoftEdge/WebView2Feedback/issues/3637),
        /// so a temporary directory is used instead to store profile data.
        /// </para>
        /// <para>
        /// The behavior depends on packaging:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>If running unpackaged: uses <see cref="Constants.CommonPaths.CefRootCacheDir"/> as the base path.</description>
        ///   </item>
        ///   <item>
        ///     <description>If running packaged (MSIX): resolves the path using <see cref="PackageUtil.ValidateAndResolvePath"/>.</description>
        ///   </item>
        ///   <item>
        ///     <description>Always returns a unique folder by appending a random name.</description>
        ///   </item>
        /// </list>
        /// </summary>
        string GetTempUserDataFolder();
    }
}
