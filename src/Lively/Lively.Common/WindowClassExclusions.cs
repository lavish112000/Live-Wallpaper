using System;
using System.Collections.Generic;

namespace Lively.Common
{
    public static class WindowClassExclusions
    {
        public static HashSet<string> DesktopClasses => new(StringComparer.OrdinalIgnoreCase)
        {
            // Desktop
            "WorkerW",
            "Progman",
            // Startmeu, taskview (win10), action center etc
            "Windows.UI.Core.CoreWindow",
            // Alt+tab screen (win10)
            "MultitaskingViewFrame",
            // Taskview (win11)
            "XamlExplorerHostIslandWindow",
            // Widget window (win11)
            "WindowsDashboard",
            // Taskbar(s)
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            // Systray notifyicon expanded popup
            "NotifyIconOverflowWindow",
            // Rainmeter widgets
            "RainmeterMeterWindow",
            // Coodesker, ref: https://github.com/rocksdanister/lively/issues/760
            "_cls_desk_"
        };
    }
}
