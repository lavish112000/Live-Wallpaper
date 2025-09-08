using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Wallpapers
{
    //Ref: 
    //https://github.com/rocksdanister/lively/discussions/342
    //https://wiki.videolan.org/documentation:modules/rc/
    public class VideoVlcPlayer : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource ctsProcessWait = new CancellationTokenSource();
        private Task<IntPtr> processWaitTask;
        private readonly Process process;
        private static int globalCount;
        private readonly int uniqueId;
        private readonly int timeOut;

        public event EventHandler Exited;
        public event EventHandler Loaded;

        public bool IsLoaded => Handle != IntPtr.Zero;

        public WallpaperType Category => Model.LivelyInfo.Type;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set; }

        public IntPtr InputHandle => IntPtr.Zero;

        public int? Pid { get; private set; } = null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public bool IsExited { get; private set; }

        public VideoVlcPlayer(string path, LibraryModel model, DisplayMonitor display, WallpaperScaler scaler = WallpaperScaler.fill, bool hwAccel = true)
        {
            var scalerArg = scaler switch
            {
                WallpaperScaler.none => "--no-autoscale ",
                WallpaperScaler.fill => "--aspect-ratio=" + display.Bounds.Width + ":" + display.Bounds.Height,
                WallpaperScaler.uniform => "--autoscale",
                WallpaperScaler.uniformFill => "--crop=" + display.Bounds.Width + ":" + display.Bounds.Height,
                _ => "--autoscale",
            };


            // Ref: https://wiki.videolan.org/VLC_command-line_help/
            var cmdArgs = new StringBuilder();
            //repeat file
            cmdArgs.Append("--loop ");
            //--no-video-title.
            cmdArgs.Append("--no-osd ");
            //video stretch algorithm.
            cmdArgs.Append(scalerArg + " ");
            //hide menus and controls.
            cmdArgs.Append("--qt-minimal-view ");
            //do not create system-tray icon.
            cmdArgs.Append("--no-qt-system-tray ");
            //prevent player window resizing to video size.
            cmdArgs.Append("--no-qt-video-autoresize ");
            //allow screensaver.
            cmdArgs.Append("--no-disable-screensaver ");
            //open window at (-9999,0), not working without: --no-embedded-video
            cmdArgs.Append("--video-x=-9999 --video-y=0 ");
            //gpu decode preference.
            cmdArgs.Append(hwAccel ? "--avcodec-hw=any " : "--avcodec-hw=none ");
            //media file path.
            cmdArgs.Append("\"" + path + "\"");

            this.process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.VlcPath),
                    UseShellExecute = false,
                    WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.PlayerPartialPaths.VlcDir),
                    Arguments = cmdArgs.ToString(),
                },
            };
            this.Model = model;
            this.Screen = display;
            this.timeOut = 20000;

            //for logging purpose
            uniqueId = globalCount++;
        }

        public async void Close()
        {
            if (IsExited)
                return;

            ctsProcessWait.TaskWaitCancel();
            while (!processWaitTask.IsTaskWaitCompleted())
                await Task.Delay(1);

            //Not reliable, app may refuse to close(open dialogue window.. etc)
            //Proc.CloseMainWindow();
            Terminate();
        }

        public WallpaperType GetWallpaperType()
        {
            return Model.LivelyInfo.Type;
        }

        public void Pause()
        {
            //todo
        }

        public void Play()
        {
            //todo
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //todo
        }

        public void SetVolume(int volume)
        {
            //todo
        }

        public void SetMute(bool mute)
        {
            //todo
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
                processWaitTask = process.WaitForProcesWindow(timeOut, ctsProcessWait.Token, true);
                this.Handle = await processWaitTask;
                await processWaitTask;
                if (Handle.Equals(IntPtr.Zero)) {
                    throw new InvalidOperationException(Properties.Resources.LivelyExceptionGeneral);
                }
                else
                {
                    //Program ready!
                    //TaskView crash fix
                    WindowUtil.BorderlessWinStyle(Handle);
                    WindowUtil.RemoveWindowFromTaskbar(Handle);
                    //todo: Restore livelyproperties.json settings here..
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
            Logger.Info($"Vlc{uniqueId}: Process exited with exit code: {process?.ExitCode}");
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
