using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    public class ExtPrograms : IWallpaper
    {
        public UInt32 SuspendCnt { get; set; }

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => Handle;

        public int? Pid { get; private set; } = null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public bool IsExited { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task<IntPtr> processWaitTask;
        private readonly Process process;
        private static int globalCount;
        private readonly int uniqueId;
        private readonly int timeOut;

        public event EventHandler Exited;
        public event EventHandler Loaded;

        /// <summary>
        /// Launch Program(.exe) Unity, godot.. as wallpaper.
        /// </summary>
        /// <param name="path">Path to program exe</param>
        /// <param name="model">Wallpaper data</param>
        /// <param name="display">Screen metadata</param>
        /// <param name="timeOut">Time to wait for program to be ready(in milliseconds)</param>
        public ExtPrograms(string path, LibraryModel model, DisplayMonitor display, int timeOut = 20000)
        {
            // Unity flags
            //-popupwindow removes from taskbar
            //-fullscreen disable fullscreen mode if set during compilation (lively is handling resizing window instead).
            //Alternative flags:
            //Unity attaches to workerw by itself; Problem: Process window handle is returning zero.
            //Examples: "-parentHWND " + workerw.ToString();// + " -popupwindow" + " -;
            //"-popupwindow -screen-fullscreen 0";
            this.process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = new()
                {
                    FileName = path,
                    UseShellExecute = false,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(path),
                    Arguments = model.LivelyInfo.Arguments,
                }
            };
            this.Model = model;
            this.Screen = display;
            this.timeOut = timeOut;
            SuspendCnt = 0;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public async void Close()
        {
            if (IsExited)
                return;

            ctsProcessWait.TaskWaitCancel();
            while(!processWaitTask.IsTaskWaitCompleted())
                await Task.Delay(1);

            //Not reliable, app may refuse to close(open dialogue window.. etc)
            //Proc.CloseMainWindow();
            Terminate();
        }

        public void Pause()
        {
            if (process != null)
            {
                //method 0, issue: does not work with every pgm
                //NativeMethods.DebugActiveProcess((uint)Proc.Id);

                //method 1, issue: resume taking time ?!
                //NativeMethods.NtSuspendProcess(Proc.Handle);

                //method 2, issue: deadlock/thread issue
                /*
                try
                {
                    ProcessSuspend.SuspendAllThreads(this);
                    //thread buggy noise otherwise?!
                    VolumeMixer.SetApplicationMute(Proc.Id, true);
                }
                catch { }
                */
            }
        }

        public void Play()
        {
            if (process != null)
            {
                //method 0, issue: does not work with every pgm
                //NativeMethods.DebugActiveProcessStop((uint)Proc.Id);

                //method 1, issue: resume taking time ?!
                //NativeMethods.NtResumeProcess(Proc.Handle);

                //method 2, issue: deadlock/thread issue
                /*
                try
                {
                    ProcessSuspend.ResumeAllThreads(this);
                    //thread buggy noise otherwise?!
                    VolumeMixer.SetApplicationMute(Proc.Id, false);
                }
                catch { }
                */
            }
        }

        public async Task ShowAsync()
        {
            if (process is null)
                return;

            try
            {
                process.Exited += Proc_Exited;
                process.Start();
                Pid = process.Id;
                processWaitTask = process.WaitForProcessOrGameWindow(Category, timeOut, ctsProcessWait.Token, true);
                this.Handle = await processWaitTask;
                if (Handle.Equals(IntPtr.Zero))
                {
                    throw new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral);
                }
                else
                {
                    //Program ready!
                    //TaskView crash fix
                    WindowUtil.BorderlessWinStyle(Handle);
                    WindowUtil.RemoveWindowFromTaskbar(Handle);
                }
                Loaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                Terminate();

                throw;
            }
        }

        private void Proc_Exited(object sender, EventArgs e)
        {
            Logger.Info($"Program{uniqueId}: Process exited with exit code: {process?.ExitCode}");
            process?.Dispose();
            IsExited = true;
            Exited?.Invoke(this, EventArgs.Empty);
        }

        public void Terminate()
        {
            if (IsExited)
                return;

            try
            {
                process.Kill();
            }
            catch { }
        }

        public void SetVolume(int volume)
        {
            try
            {
                VolumeMixerUtil.SetApplicationVolume(process.Id, volume);
            }
            catch { }
        }

        public void SetMute(bool mute)
        {
            //todo
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
        }

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            //todo
        }

        public void Dispose()
        {
            // Process object is disposed in Exit event.
            Terminate();
        }
    }
}
