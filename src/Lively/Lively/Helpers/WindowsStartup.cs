using Lively.Common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Lively.Helpers
{
    public static class WindowsStartup
    {
        public async static Task<StartupTaskState> TrySetStartup(bool isStartWithWindow)
        {
            var result = StartupTaskState.Disabled;
            try
            {
                if (PackageUtil.IsRunningAsPackaged)
                    result = await SetStartupTask(isStartWithWindow);
                else
                    SetStartupRegistry(isStartWithWindow);
            }
            catch
            {
                return result;
            }
            return result;
        }

        /// <summary>
        /// Adds startup entry in registry under application name "livelywpf", current user ONLY. (Does not require admin rights).
        /// </summary>
        /// <param name="isStartWithWindows">Add or delete entry.</param>
        private static void SetStartupRegistry(bool isStartWithWindows = false)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            try
            {
                if (isStartWithWindows)
                {
                    key.SetValue(curAssembly.GetName().Name, "\"" + Path.ChangeExtension(curAssembly.Location, ".exe") + "\"");
                }
                else
                {
                    key.DeleteValue(curAssembly.GetName().Name, false);
                }
            }
            finally
            {
                key.Close();
            }
        }

        //ref: https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.startuptask?view=winrt-19041
        private async static Task<StartupTaskState> SetStartupTask(bool isStartWithWindows = false)
        {
            // Pass the task ID you specified in the appxmanifest file
            StartupTask startupTask = await StartupTask.GetAsync("AppStartup");
            var startupState = startupTask.State;
            switch (startupState)
            {
                case StartupTaskState.Disabled:
                    {
                        // Task is disabled but can be enabled.
                        // ensure that you are on a UI thread when you call RequestEnableAsync()
                        if (isStartWithWindows)
                            startupState = await startupTask.RequestEnableAsync();
                    }
                    break;
                case StartupTaskState.DisabledByUser:
                    {
                        // Task is disabled and user must enable it manually.
                    }
                    break;
                case StartupTaskState.DisabledByPolicy:
                    {
                        // Startup disabled by group policy, or not supported on this device.
                    }
                    break;
                case StartupTaskState.Enabled:
                    {
                        if (!isStartWithWindows)
                            startupTask.Disable();
                    }
                    break;
                    default:
                    {
                        if (isStartWithWindows)
                            startupState = await startupTask.RequestEnableAsync();
                    }
                    break;
            }
            return startupState;
        }
    }
}
