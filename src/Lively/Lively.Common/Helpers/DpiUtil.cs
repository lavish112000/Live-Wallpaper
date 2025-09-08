using Lively.Common.Helpers.Pinvoke;
using System;

namespace Lively.Common.Helpers
{
    public static class DpiUtil
    {
        /// <summary>
        /// Attempts to get the DPI scaling factor for the given monitor.
        /// This method will fail if the current thread not is running in a Per-Monitor DPI-aware context.
        /// </summary>
        public static bool TryGetDisplayScale(IntPtr hmonitor, out double scale)
        {
            scale = 1d;

            // GetDpiForMonitor or GetDpiForWindow works correctly only if the calling process has DPI_AWARENESS_PER_MONITOR_AWARE.
            // Ref: https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor
            if (GetThreadDpiAwareness() != NativeMethods.DPI_AWARENESS.PER_MONITOR_AWARE)
                return false;

            if (NativeMethods.GetDpiForMonitor(hmonitor,
                NativeMethods.MonitorDpiType.MDT_Effective_DPI,
                out uint dpiX,
                out _) == 0)
            {
                scale = dpiX / 96d;
                return true;
            }
            return false;
        }

        public static bool TryGetWindowScale(IntPtr hwnd, out double scale)
        {
            scale = 1d;

            // GetDpiForMonitor or GetDpiForWindow works correctly only if the calling process has DPI_AWARENESS_PER_MONITOR_AWARE.
            // Ref: https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor
            if (GetThreadDpiAwareness() != NativeMethods.DPI_AWARENESS.PER_MONITOR_AWARE)
                return false;

            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            if (dpi != 0)
            {
                scale = dpi / 96d;
                return true;
            }
            return false;
        }

        public static NativeMethods.DPI_AWARENESS GetThreadDpiAwareness()
        {
            var context = NativeMethods.GetThreadDpiAwarenessContext();
            return NativeMethods.GetAwarenessFromDpiAwarenessContext(context);
        }
    }
}
