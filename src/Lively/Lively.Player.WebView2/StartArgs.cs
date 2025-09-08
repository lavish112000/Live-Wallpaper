using CommandLine;
using Lively.Models.Enums;

namespace Lively.Player.WebView2
{
    public class StartArgs
    {
        [Option("wallpaper-url",
        Required = true,
        HelpText = "The url/html-file to load.")]
        public string Url { get; set; }

        [Option("wallpaper-property",
        Required = false,
        Default = null,
        HelpText = "LivelyProperties.info filepath (SaveData/wpdata).")]
        public string Properties { get; set; }

        [Option("wallpaper-type",
        Required = true,
        HelpText = "Type of wallpaper.")]
        public WebPageType Type { get; set; }

        [Option("wallpaper-display",
        Required = false,
        HelpText = "Wallpaper running display.")]
        public string DisplayDevice { get; set; }

        [Option("wallpaper-geometry",
        Required = false,
        HelpText = "Window size (WxH).")]
        public string Geometry { get; set; }

        [Option("wallpaper-scale",
        Required = false,
        HelpText = "Wallpaper scale factor.")]
        public double? Scale { get; set; }

        [Option("wallpaper-audio",
        Default = false,
        HelpText = "Analyse system audio(visualiser data.)")]
        public bool AudioVisualizer { get; set; }

        [Option("wallpaper-debug",
        Required = false,
        HelpText = "Debugging port.")]
        public string DebugPort { get; set; }

        [Option("wallpaper-user-data",
        Required = false,
        HelpText = "WebView2 user data path")]
        public string UserDataPath { get; set; }

        [Option("wallpaper-volume",
        Required = false,
        Default = 100,
        HelpText = "Audio volume.")]
        public int Volume { get; set; }

        [Option("wallpaper-system-information",
        Default = false,
        Required = false,
        HelpText = "Lively hw monitor api.")]
        public bool SysInfo { get; set; }

        [Option("wallpaper-system-nowplaying", 
        Default = false, 
        Required = false)]
        public bool NowPlaying { get; set; }

        [Option("wallpaper-pause-event",
        Required = false,
        HelpText = "Wallpaper playback changed notify.")]
        public bool PauseEvent { get; set; }

        [Option("wallpaper-pause-media",
        Required = false,
        HelpText = "Try to pause all webpage media when wallpaper pause")]
        public bool PauseWebMedia { get; set; }

        [Option("wallpaper-verbose-log",
        Required = false,
        HelpText = "Verbose Logging.")]
        public bool VerboseLog { get; set; }

        [Option("wallpaper-color-scheme",
        Required = false,
        HelpText = "Set PreferredColorScheme.")]
        public AppTheme Theme { get; set; }
    }
}
