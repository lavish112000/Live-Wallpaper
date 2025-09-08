using Lively.Models;
using System;
using System.Diagnostics;

namespace Lively.Common.Factories
{
    public interface IApplicationsFactory
    {
        ApplicationModel CreateApp(Process process);
        ApplicationModel CreateApp(IntPtr hwnd);
        ApplicationModel CreateApp(string path);
    }
}