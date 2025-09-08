using Lively.Common.Helpers.Pinvoke;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Lively.Common.Helpers;

public static class PackageUtil
{
    private const uint FILE_READ_EA = 0x0008;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const long APPMODEL_ERROR_NO_PACKAGE = 15700;
    private const uint ERROR_SUCCESS = 0;

    public static bool IsRunningAsPackaged { get; } = IsPackaged();

    public static string GetPackageFamilyName()
    {
        if (!IsRunningAsPackaged)
            throw new InvalidOperationException("Not running as packaged.");

        int length = 0;
        var packageFullName = new StringBuilder(0);
        _ = NativeMethods.GetCurrentPackageFullName(ref length, packageFullName);
        packageFullName = new StringBuilder(length);
        var hr = NativeMethods.GetCurrentPackageFullName(ref length, packageFullName);
        if (hr != ERROR_SUCCESS)
            throw new Win32Exception(hr);

        return packageFullName.ToString();
    }

    public static string GetPackagedLocalAppDataPath()
    {
        if (!IsRunningAsPackaged)
            throw new InvalidOperationException("Not running as packaged.");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packageFamilyName = GetPackageFamilyName();
        return Path.Combine(localAppData, "Packages", packageFamilyName, "LocalCache", "Local", Constants.ApplicationType.Name);
    }

    /// <summary>
    /// Resolves the real filesystem path for the given <paramref name="path"/>.
    /// - In unpackaged (non-MSIX) mode: simply checks if the file or directory exists and returns the same path, 
    ///   otherwise throws <see cref="FileNotFoundException"/>.
    /// - In packaged (MSIX) mode: resolves the virtualized path. If both the virtualized and the real path exist, 
    ///   the result depends on what <c>GetFinalPathNameByHandle</c> returns.
    /// </summary>
    public static string ValidateAndResolvePath(string path)
    {
        if (!IsRunningAsPackaged)
        {
            if (File.Exists(path) || Directory.Exists(path))
                return path;

            throw new FileNotFoundException();
        }

        // Caller must have <longPathAware>true in the manifest; otherwise prepend @"\\?\"
        // True by default on .NET Core 5+.
        using var handle = NativeMethods.CreateFile(path,
            FILE_READ_EA,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);

        if (handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return GetFinalPath(handle);
    }

    private static bool IsPackaged()
    {
        var length = 0;
        var packageFullName = new StringBuilder(0);
        _ = NativeMethods.GetCurrentPackageFullName(ref length, packageFullName);
        packageFullName = new StringBuilder(length);
        var hr = NativeMethods.GetCurrentPackageFullName(ref length, packageFullName);
        return hr != APPMODEL_ERROR_NO_PACKAGE;
    }

    private static string GetFinalPath(SafeFileHandle handle)
    {
        var sb = new StringBuilder(512);
        uint result = NativeMethods.GetFinalPathNameByHandle(handle, sb, (uint)sb.Capacity, 0);
        if (result == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        // if buffer was too small, retry with exact size
        if (result > sb.Capacity)
        {
            sb.Capacity = (int)result;
            result = NativeMethods.GetFinalPathNameByHandle(handle, sb, (uint)sb.Capacity, 0);
            if (result == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Windows prepends "\\?\" to long paths.
        var prefix = @"\\?\";
        var finalPath = sb.ToString();
        return finalPath.StartsWith(prefix) ? finalPath.Substring(prefix.Length) : finalPath;
    }
}
