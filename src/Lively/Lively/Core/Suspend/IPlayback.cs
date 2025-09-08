using Lively.Models;
using Lively.Models.Enums;
using System;

namespace Lively.Core.Suspend
{
    public interface IPlayback : IDisposable
    {
        void Start();
        void Stop();
        IDisposable DeferPlayback();

        PlaybackPolicy WallpaperPlaybackPolicy { get; set; }

        event EventHandler<PlaybackPolicy> PlaybackPolicyChanged;
        event EventHandler<WallpaperControlEventArgs> WallpaperControlChanged;
    }

    public class WallpaperControlEventArgs : EventArgs
    {
        public WallpaperControlAction Action { get; }
        public DisplayMonitor Display { get; } // null for all displays
        public int? Volume { get; }

        public WallpaperControlEventArgs(WallpaperControlAction action, DisplayMonitor display = null, int? volume = null)
        {
            Action = action;
            Display = display;
            Volume = volume;
        }
    }

    public enum WallpaperControlAction
    {
        Pause,
        Play,
        SetVolume
    }
}