using Microsoft.Win32;
using System;
using System.IO;

namespace Lively.Common.Helpers
{
    public static class ScreensaverUtil
    {
        public static string GetCurrentScreensaver()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);
            if (key?.GetValue("SCRNSAVE.EXE") is string path)
                return Path.GetFileNameWithoutExtension(path);
            return string.Empty;
        }

        public static bool IsScreensaverSelected(string name)
        {
            try
            {
                var currentScreensaver = GetCurrentScreensaver();
                return string.Equals(currentScreensaver, name, StringComparison.OrdinalIgnoreCase);
            }
            catch { /* Nothing to do */ }
            return false;
        }
    }
}
