using Linearstar.Windows.RawInput;
using Lively.Common.Helpers.Pinvoke;
using Lively.Models.Enums;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Lively.Views.WindowMsg
{
    /// <summary>
    /// DirectX rawinput hook.
    /// Ref: https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input
    /// </summary>
    public partial class RawInputMsgWindow : Window
    {
        public InputForwardMode InputMode { get; private set; }
        //public events
        public event EventHandler<MouseRawArgs> MouseMoveRaw;
        public event EventHandler<MouseClickRawArgs> MouseDownRaw;
        public event EventHandler<MouseClickRawArgs> MouseUpRaw;
        public event EventHandler<KeyboardClickRawArgs> KeyboardClickRaw;

        public RawInputMsgWindow()
        {
            InitializeComponent();

            this.InputMode = InputForwardMode.mousekeyboard;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            switch (InputMode)
            {
                case InputForwardMode.off:
                    this.Close();
                    break;
                case InputForwardMode.mouse:
                    //ExInputSink flag makes it work even when not in foreground and async..
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    break;
                case InputForwardMode.mousekeyboard:
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    break;
            }

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(Hook);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch (InputMode)
            {
                case InputForwardMode.off:
                    break;
                case InputForwardMode.mouse:
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
                    break;
                case InputForwardMode.mousekeyboard:
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
                    break;
            }
        }

        protected IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // You can read inputs by processing the WM_INPUT message.
            if (msg == (int)NativeMethods.WM.INPUT)
            {
                // Create an RawInputData from the handle stored in lParam.
                var data = RawInputData.FromHandle(lparam);

                //You can identify the source device using Header.DeviceHandle or just Device.
                //var sourceDeviceHandle = data.Header.DeviceHandle;
                //var sourceDevice = data.Device;

                // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
                // They contain the raw input data in their properties.
                switch (data)
                {
                    case RawInputMouseData mouse:
                        //RawInput only gives relative mouse movement value.. 
                        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT P))
                        {
                            break;
                        }

                        switch (mouse.Mouse.Buttons)
                        {
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonDown:
                                {
                                    MouseDownRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.left));
                                }
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonUp:
                                {
                                    MouseUpRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.left));
                                }
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonDown:
                                {
                                    MouseDownRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.right));
                                }
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonUp:
                                {
                                    MouseUpRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.right));
                                }
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None:
                                {
                                    MouseMoveRaw?.Invoke(this, new MouseRawArgs(P.X, P.Y));
                                }
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MouseWheel:
                                {
                                    //Disabled, not tested yet.
                                    /*
                                    https://github.com/ivarboms/game-engine/blob/master/Input/RawInput.cpp
                                    Mouse wheel deltas are represented as multiples of 120.
                                    MSDN: The delta was set to 120 to allow Microsoft or other vendors to build
                                    finer-resolution wheels (a freely-rotating wheel with no notches) to send more
                                    messages per rotation, but with a smaller value in each message.
                                    Because of this, the value is converted to a float in case a mouse's wheel
                                    reports a value other than 120, in which case dividing by 120 would produce
                                    a very incorrect value.
                                    More info: http://social.msdn.microsoft.com/forums/en-US/gametechnologiesgeneral/thread/1deb5f7e-95ee-40ac-84db-58d636f601c7/
                                    */

                                    /*
                                    // One wheel notch is represented as this delta (WHEEL_DELTA).
                                    const float oneNotch = 120;

                                    // Mouse wheel delta in multiples of WHEEL_DELTA (120).
                                    float mouseWheelDelta = mouse.Mouse.RawButtons;

                                    // Convert each notch from [-120, 120] to [-1, 1].
                                    mouseWheelDelta = mouseWheelDelta / oneNotch;

                                    MouseScrollSimulate(mouseWheelDelta);
                                    */
                                }
                                break;
                        }
                        break;
                    case RawInputKeyboardData keyboard:
                        {
                            KeyboardClickRaw?.Invoke(this,
                                new KeyboardClickRawArgs((int)keyboard.Keyboard.WindowMessage,
                                    (IntPtr)keyboard.Keyboard.VirutalKey,
                                    keyboard.Keyboard.ScanCode,
                                    (keyboard.Keyboard.Flags != Linearstar.Windows.RawInput.Native.RawKeyboardFlags.Up)));
                        }
                        break;
                }
            }
            return IntPtr.Zero;
        }
    }

    public enum RawInputMouseBtn
    {
        left,
        right
    }

    public class MouseRawArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }
        public MouseRawArgs(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class MouseClickRawArgs : MouseRawArgs
    {
        public RawInputMouseBtn Button { get; }
        public MouseClickRawArgs(int x, int y, RawInputMouseBtn btn) : base(x, y)
        {
            Button = btn;
        }
    }

    public class KeyboardClickRawArgs : EventArgs
    {
        /// <summary>
        /// The Windows message (WM_KEYDOWN, WM_KEYUP, etc.)
        /// </summary>
        public int WindowMessage { get; }

        /// <summary>
        /// The virtual key code.
        /// </summary>
        public IntPtr VirtualKey { get; }

        /// <summary>
        /// The hardware scan code.
        /// </summary>
        public int ScanCode { get; }

        /// <summary>
        /// True if this is a key down, false if key up.
        /// </summary>
        public bool IsKeyDown { get; }

        public KeyboardClickRawArgs(int windowMessage, IntPtr virtualKey, int scanCode, bool isKeyDown)
        {
            WindowMessage = windowMessage;
            VirtualKey = virtualKey;
            ScanCode = scanCode;
            IsKeyDown = isKeyDown;
        }
    }
}
