using CefSharp;
using CefSharp.SchemeHandler;
using CefSharp.WinForms;
using CommandLine;
using Lively.Common.Helpers;
using Lively.Common.JsonConverters;
using Lively.Common.Services;
using Lively.Models.Enums;
using Lively.Models.Message;
using Lively.Player.CefSharp.Extensions.CefSharp;
using Lively.Player.CefSharp.Extensions.CefSharp.DevTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lively.Common.Extensions;

namespace Lively.Player.CefSharp
{
    public partial class Form1 : Form
    {
        private bool isPaused = false;
        private ChromiumWebBrowser chromeBrowser;
        private StartArgs startArgs;

        private bool initializedServices = false; //delay API init till loaded page
        private IHardwareUsageService hardwareUsageService;
        private IAudioVisualizerService audioVisualizerService;
        private INowPlayingService nowPlayingService;

        private AppTheme SystemTheme { get; } = ThemeUtil.GetWindowsTheme();
        private bool IsDebugging { get; } = BuildInfoUtil.IsDebugBuild();

        public Form1()
        {
            InitializeComponent();
            if (IsDebugging)
            {
                startArgs = new StartArgs
                {
                    // .html fullpath
                    Url = "https://google.com/",
                    //online or local(file)
                    Type = WebPageType.online,
                    // LivelyProperties.json path if any
                    Properties = @"",
                    SysInfo = false,
                    NowPlaying = false,
                    AudioVisualizer = false,
                    PauseEvent = false
                };

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(1920, 1080);
                this.ShowInTaskbar = true;
                this.MaximizeBox = true;
                this.MinimizeBox = true;
            }
            else
            {
                Parser.Default.ParseArguments<StartArgs>(Environment.GetCommandLineArgs())
                    .WithParsed((x) => startArgs = x)
                    .WithNotParsed(HandleParseError);

                this.WindowState = FormWindowState.Minimized;
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(-9999, 0);

                if (startArgs.Geometry != null)
                {
                    var msg = startArgs.Geometry.Split('x');
                    if (msg.Length >= 2 && int.TryParse(msg[0], out int width) && int.TryParse(msg[1], out int height))
                    {
                        this.Size = new Size(width, height);
                    }
                }

                var darkColor = Color.FromArgb(30, 30, 30);
                var lightColor = Color.FromArgb(240, 240, 240);
                this.BackColor = startArgs.Theme switch
                {
                    AppTheme.Auto => SystemTheme == AppTheme.Dark ? darkColor : lightColor,
                    AppTheme.Light => lightColor,
                    AppTheme.Dark => darkColor,
                    _ => darkColor,
                };
            }

            try 
            {
                InitializeCefSharp();
            }
            catch (Exception ex) {
                ex.SendError(SendToParent, "Failed to initialize CefSharp");
            }
            finally {
                _ = ListenToParent();
            }
        }

        // Hide from taskview and taskbar.
        // ShowInTaskbar = true does not create TOOLWINDOW.
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private void HandleParseError(IEnumerable<Error> errs)
        {
            if (errs != null)
                string.Join(Environment.NewLine, errs).SendError(SendToParent, "Error parsing launch arguments");

            // ERROR_INVALID_PARAMETER
            // Ref: <https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499->
            Environment.Exit(87);
        }

        #region ipc

        public class WallpaperPlaybackState
        {
            public bool IsPaused { get; set; }
        }

        /// <summary>
        /// std I/O redirect, used to communicate with lively. 
        /// </summary>
        public async Task ListenToParent()
        {
            if (IsDebugging)
                return;

            var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        // Since UTF8 is backward compatible, will work without this reader for non unicode characters.
                        string text = await reader.ReadLineAsync();
                        if (startArgs.VerboseLog)
                        {
                            Console.WriteLine(text);
                        }

                        if (string.IsNullOrEmpty(text))
                        {
                            //When the redirected stream is closed, a null line is sent to the event handler. 
                            break;
                        }
                        else
                        {
                            try
                            {
                                var close = false;
                                var obj = JsonConvert.DeserializeObject<IpcMessage>(text, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                                switch (obj.Type)
                                {
                                    case MessageType.cmd_reload:
                                        chromeBrowser?.Reload(true);
                                        break;
                                    case MessageType.cmd_suspend:
                                        if (chromeBrowser.CanExecuteJavascriptInMainFrame && startArgs.PauseEvent && !isPaused) //if js context ready
                                        {
                                            chromeBrowser.ExecuteScriptAsync("livelyWallpaperPlaybackChanged",
                                                JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = true }),
                                                Formatting.Indented);
                                        }
                                        isPaused = true;
                                        break;
                                    case MessageType.cmd_resume:
                                        if (chromeBrowser.CanExecuteJavascriptInMainFrame && isPaused)
                                        {
                                            if (startArgs.PauseEvent)
                                            {
                                                chromeBrowser.ExecuteScriptAsync("livelyWallpaperPlaybackChanged",
                                                    JsonConvert.SerializeObject(new WallpaperPlaybackState() { IsPaused = false }),
                                                    Formatting.Indented);
                                            }

                                            if (startArgs.NowPlaying)
                                            {
                                                //update media state
                                                chromeBrowser.ExecuteScriptAsync("livelyCurrentTrack", JsonConvert.SerializeObject(nowPlayingService?.CurrentTrack, Formatting.Indented));
                                            }
                                        }
                                        isPaused = false;
                                        break;
                                    case MessageType.cmd_volume:
                                        var vc = (LivelyVolumeCmd)obj;
                                        chromeBrowser.GetBrowserHost()?.SetAudioMuted(vc.Volume == 0);
                                        break;
                                    case MessageType.cmd_screenshot:
                                        var success = true;
                                        var scr = (LivelyScreenshotCmd)obj;
                                        try
                                        {
                                            await chromeBrowser.CaptureScreenshot(scr.Format, scr.FilePath);
                                        }
                                        catch (Exception ie)
                                        {
                                            success = false;
                                            ie.SendError(SendToParent, "Failed to capture screenshot");
                                        }
                                        finally
                                        {
                                            SendToParent(new LivelyMessageScreenshot()
                                            {
                                                FileName = Path.GetFileName(scr.FilePath),
                                                Success = success
                                            });
                                        }
                                        break;
                                    case MessageType.lp_slider:
                                        var sl = (LivelySlider)obj;
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", sl.Name, sl.Value);
                                        break;
                                    case MessageType.lp_textbox:
                                        var tb = (LivelyTextBox)obj;
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", tb.Name, tb.Value);
                                        break;
                                    case MessageType.lp_dropdown:
                                        var dd = (LivelyDropdown)obj;
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", dd.Name, dd.Value);
                                        break;
                                    case MessageType.lp_cpicker:
                                        var cp = (LivelyColorPicker)obj;
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", cp.Name, cp.Value);
                                        break;
                                    case MessageType.lp_chekbox:
                                        var cb = (LivelyCheckbox)obj;
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", cb.Name, cb.Value);
                                        break;
                                    case MessageType.lp_fdropdown:
                                        var fd = (LivelyFolderDropdown)obj;
                                        var filePath = fd.Value is null ? null : Path.Combine(Path.GetDirectoryName(startArgs.Url), fd.Value);
                                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", fd.Name, File.Exists(filePath) ? fd.Value : null);
                                        break;
                                    case MessageType.lp_button:
                                        var btn = (LivelyButton)obj;
                                        if (btn.IsDefault)
                                        {
                                            RestoreLivelyProperties(startArgs.Properties);
                                        }
                                        else
                                        {
                                            chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", btn.Name, true);
                                        }
                                        break;
                                    case MessageType.lsp_perfcntr:
                                        if (chromeBrowser.CanExecuteJavascriptInMainFrame) //if js context ready
                                        {
                                            chromeBrowser.ExecuteScriptAsync("livelySystemInformation", JsonConvert.SerializeObject(((LivelySystemInformation)obj).Info, Formatting.Indented));
                                        }
                                        break;
                                    case MessageType.lsp_nowplaying:
                                        if (chromeBrowser.CanExecuteJavascriptInMainFrame)
                                        {
                                            chromeBrowser.ExecuteScriptAsync("livelyCurrentTrack", JsonConvert.SerializeObject(((LivelySystemNowPlaying)obj).Info, Formatting.Indented));
                                        }
                                        break;
                                    case MessageType.cmd_close:
                                        close = true;
                                        break;
                                }

                                if (close)
                                {
                                    break;
                                }
                            }
                            catch (Exception ie)
                            {
                                ie.SendError(SendToParent);
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                e.SendError(SendToParent);
            }
            finally
            {
                reader?.Dispose();
                Application.Exit();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            audioVisualizerService?.Dispose();
            hardwareUsageService?.Stop();
            nowPlayingService?.Stop();
            chromeBrowser?.Dispose();
            Cef.Shutdown();
        }

        private void SendToParent(IpcMessage obj)
        {
            if (!IsDebugging)
                Console.WriteLine(JsonConvert.SerializeObject(obj));

            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }

        #endregion //ipc

        #region cef

        /// <summary>
        /// starts up & loads cef instance.
        /// </summary>
        public void InitializeCefSharp()
        {
            CefSettings settings = new CefSettings
            {
                // Disable chromium from parsing global command line.
                // Ref: https://github.com/cefsharp/CefSharp/discussions/4618
                CommandLineArgsDisabled = true,
                // Required >120.1.80, otherwise crash when multiple instance.
                // Ref: https://github.com/cefsharp/CefSharp/issues/4668
                RootCachePath = Path.Combine(Path.GetTempPath(), "Lively Wallpaper", "CEF", Path.GetRandomFileName())
            };
            //ref: https://magpcss.org/ceforum/apidocs3/projects/(default)/_cef_browser_settings_t.html#universal_access_from_file_urls
            //settings.CefCommandLineArgs.Add("allow-universal-access-from-files", "1"); //UNSAFE, Testing Only!
            if (startArgs.Volume == 0)
                settings.CefCommandLineArgs.Add("mute-audio", "1");
            //auto-play video without it being muted (default cef behaviour is overriden.)
            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            settings.LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lively Wallpaper", "Cef", "logfile.txt");

            if (!string.IsNullOrWhiteSpace(startArgs.DebugPort) && int.TryParse(startArgs.DebugPort, out int value))
                settings.RemoteDebuggingPort = value;

            if (!string.IsNullOrWhiteSpace(startArgs.CachePath))
                settings.CachePath = startArgs.CachePath;
            else
                //Creates GPUCache regardless even if disk CachePath is not set!
                settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache");

            switch (startArgs.Type)
            {
                case WebPageType.online:
                    {
                        string tmp = null;
                        Cef.Initialize(settings);
                        if (StreamUtil.TryParseShadertoy(startArgs.Url, ref tmp))
                        {
                            chromeBrowser = new ChromiumWebBrowser(string.Empty);
                            chromeBrowser.LoadHtml(tmp);
                        }
                        else if (StreamUtil.TryParseYouTubeVideoIdFromUrl(startArgs.Url, ref tmp))
                        {
                            chromeBrowser = new ChromiumWebBrowser(string.Empty);
                            chromeBrowser.LoadHtml($"https://www.youtube.com/embed/{tmp}?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist={tmp}");
                        }
                        else
                        {
                            chromeBrowser = new ChromiumWebBrowser(startArgs.Url);
                        }
                    }
                    break;
                case WebPageType.local:
                    {
                        settings.RegisterScheme(new CefCustomScheme
                        {
                            SchemeName = "localfolder",
                            IsFetchEnabled = true,
                            SchemeHandlerFactory = new FolderSchemeHandlerFactory
                            (
                                rootFolder: Path.GetDirectoryName(startArgs.Url),
                                hostName: Path.GetFileName(startArgs.Url),
                                defaultPage: Path.GetFileName(startArgs.Url)//"index.html" // will default to index.html
                            )

                        });
                        Cef.Initialize(settings);
                        chromeBrowser = new ChromiumWebBrowser($"localfolder://{Path.GetFileName(startArgs.Url)}");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            //cef right click contextmenu disable.
            chromeBrowser.MenuHandler = new MenuHandler();
            //disable links starting in new cef window.
            chromeBrowser.LifeSpanHandler = new PopUpHandle();

            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            chromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged1;
            chromeBrowser.LoadingStateChanged += ChromeBrowser_LoadingStateChanged;
            chromeBrowser.LoadError += ChromeBrowser_LoadError;
            chromeBrowser.TitleChanged += ChromeBrowser_TitleChanged;
            chromeBrowser.ConsoleMessage += ChromeBrowser_ConsoleMessage;
        }

        private void ChromeBrowser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            $"{e.Message}, source: {e.Source} ({e.Line})".SendConsole(SendToParent);
        }

        private void ChromeBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            this.Invoke((MethodInvoker)(() => this.Text = e.Title));
        }

        private void ChromeBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
                return;

            RestoreLivelyProperties(startArgs.Properties);
            SendToParent(new LivelyMessageWallpaperLoaded() { Success = true });

            if (!initializedServices)
            {
                initializedServices = true;
                if (startArgs.AudioVisualizer)
                {
                    audioVisualizerService = new NAudioVisualizerService();
                    audioVisualizerService.AudioDataAvailable += (s, data) =>
                    {
                        try
                        {
                            if (isPaused)
                                return;

                            if (chromeBrowser.CanExecuteJavascriptInMainFrame) //if js context ready
                            {
                                ExecuteScriptFunctionAsync("livelyAudioListener", data);
                            }
                        }
                        catch (Exception)
                        {
                            //TODO
                        }
                    };
                    audioVisualizerService.Start();
                }

                if (startArgs.NowPlaying)
                {
                    nowPlayingService = new NpsmNowPlayingService();
                    nowPlayingService.NowPlayingTrackChanged += (s, data) => {
                        try
                        {
                            if (isPaused)
                                return;

                            if (chromeBrowser.CanExecuteJavascriptInMainFrame) //if js context ready
                            {
                                chromeBrowser.ExecuteScriptAsync("livelyCurrentTrack", JsonConvert.SerializeObject(data, Formatting.Indented));
                            }
                        }
                        catch (Exception ex)
                        {
                            //TODO

                        }
                    };
                    nowPlayingService.Start();
                }

                if (startArgs.SysInfo)
                {
                    hardwareUsageService = new HardwareUsageService();
                    hardwareUsageService.HWMonitor += (s, data) =>
                    {
                        try
                        {
                            if (isPaused)
                                return;

                            if (chromeBrowser.CanExecuteJavascriptInMainFrame) //if js context ready
                            {
                                chromeBrowser.ExecuteScriptAsync("livelySystemInformation", JsonConvert.SerializeObject(data, Formatting.Indented));
                            }
                        }
                        catch
                        {
                            //TODO
                        }
                    };
                    hardwareUsageService.Start();
                }
            }
        }

        private void RestoreLivelyProperties(string propertyPath)
        {
            try
            {
                _ = LivelyPropertyUtil.LoadProperty(propertyPath, Path.GetDirectoryName(startArgs.Url), (key, value) =>
                {
                    if (chromeBrowser.CanExecuteJavascriptInMainFrame)
                        chromeBrowser.ExecuteScriptAsync("livelyPropertyListener", key, value);

                    return Task.FromResult(0);
                });
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent);
            }
        }

        private async void ChromeBrowser_IsBrowserInitializedChanged1(object sender, EventArgs e)
        {
            //sends cefsharp handle to lively. (this is a subprocess of this application, so simply searching process.mainwindowhandle won't help.)
            SendToParent(new LivelyMessageHwnd()
            {
                Hwnd = chromeBrowser.GetBrowser().GetHost().GetWindowHandle().ToInt32()
            });


            try
            {
                var pageTheme = startArgs.Theme == AppTheme.Auto ? SystemTheme : startArgs.Theme;
                if (pageTheme == AppTheme.Dark)
                {
                    // Ref: https://github.com/cefsharp/CefSharp/discussions/3955
                    using var devToolsClient = chromeBrowser.GetDevToolsClient();
                    await devToolsClient.Emulation.SetAutoDarkModeOverrideAsync(true);
                }
            }
            catch (Exception ex)
            {
                ex.SendError(SendToParent, "Failed to set page theme");
            }
        }

        private void ChromeBrowser_LoadError(object sender, LoadErrorEventArgs e)
        {
            Debug.WriteLine("Error Loading Page:-" + e.ErrorText);  //ERR_BLOCKED_BY_RESPONSE, likely missing audio/video codec error for youtube.com?
            if (startArgs.Type == WebPageType.local || e.ErrorCode == CefErrorCode.Aborted || e.ErrorCode == (CefErrorCode)(-27))//e.ErrorCode == CefErrorCode.NameNotResolved || e.ErrorCode == CefErrorCode.InternetDisconnected   || e.ErrorCode == CefErrorCode.NetworkAccessDenied || e.ErrorCode == CefErrorCode.NetworkIoSuspended)
            {
                //ignoring some error's.
                return;
            }
            chromeBrowser.LoadHtml(@"<head> <meta charset=""utf - 8""> <title>Error</title>  <style>
            * { line-height: 1.2; margin: 0; } html { display: table; font-family: sans-serif; height: 100%; text-align: center; width: 100%; } body { background-color: #252525; display:
            table-cell; vertical-align: middle; margin: 2em auto; } h1 { color: #e5e5e5; font-size: 2em; font-weight: 400; } p { color: #cccccc; margin: 0 auto; width: 280px; } .url{color: #e5e5e5; position: absolute; margin: 16px; right: 0; top: 0; } @media only
            screen and (max-width: 280px) { body, p { width: 95%; } h1 { font-size: 1.5em; margin: 0 0 0.3em; } } </style></head><body><div class=""url"">" + startArgs.Url + "</div> <h1>Unable to load webpage :'(</h1> <p>" + e.ErrorText + "</p></body></html>");
            //chromeBrowser.LoadHtml(@"<body style=""background-color:black;""><h1 style = ""color:white;"">Error Loading webpage:" + e.ErrorText + "</h1></body>");            
        }

        #endregion //cef

        /// <summary>
        /// Supports arrays
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="parameters"></param>
        private void ExecuteScriptFunctionAsync(string functionName, params object[] parameters)
        {
            var script = new StringBuilder();
            script.Append(functionName);
            script.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                script.Append(JsonConvert.SerializeObject(parameters[i]));
                if (i < parameters.Length - 1)
                {
                    script.Append(", ");
                }
            }
            script.Append(");");
            chromeBrowser?.ExecuteScriptAsync(script.ToString());
        }
    }
}
