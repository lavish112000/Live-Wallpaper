using System;

namespace Lively.Core.Suspend;

public partial class Playback
{
    private sealed class PlaybackDeferrer : IDisposable
    {
        private readonly Playback playback;
        private readonly bool wasEnabled;

        public PlaybackDeferrer(Playback playback)
        {
            this.playback = playback;

            wasEnabled = playback.dispatcherTimer.IsEnabled;
            playback.dispatcherTimer.Stop();
            playback.PlayWallpapers();
        }

        public void Dispose()
        {
            if (wasEnabled)
                playback.dispatcherTimer.Start();
        }
    }
}
