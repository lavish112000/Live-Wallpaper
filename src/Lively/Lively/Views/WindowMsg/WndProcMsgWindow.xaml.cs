using Lively.Common.Helpers.Pinvoke;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Lively.Views.WindowMsg
{
    public partial class WndProcMsgWindow : Window
    {
        public event EventHandler TaskbarCreated;
        public event EventHandler<WindowMessageEventArgs> WindowMessageReceived;

        private readonly int WM_TASKBARCREATED = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        public WndProcMsgWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TASKBARCREATED)
            {
                TaskbarCreated?.Invoke(this, EventArgs.Empty);

                return IntPtr.Zero;
            }
            else
            {
                var args = new WindowMessageEventArgs(hwnd, (uint)msg, wParam, lParam);
                WindowMessageReceived?.Invoke(this, args);

                return args.Result;
            }
        }
    }

    public class WindowMessageEventArgs : EventArgs
    {
        public IntPtr Hwnd { get; }
        public uint Message { get; }
        public IntPtr WParam { get; }
        public IntPtr LParam { get; }
        public IntPtr Result { get; set; } = IntPtr.Zero;

        public WindowMessageEventArgs(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            Hwnd = hwnd;
            Message = message;
            WParam = wParam;
            LParam = lParam;
        }
    }
}
