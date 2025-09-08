using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using Lively.Extensions;
using Lively.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Lively.Views
{
    public partial class LibraryPreview : Window
    {
        private readonly LibraryPreviewViewModel viewModel;

        public LibraryPreview(IWallpaper wallpaper)
        {
            InitializeComponent();
            this.viewModel = App.Services.GetRequiredService<LibraryPreviewViewModel>();
            this.DataContext = viewModel;
            this.viewModel.Wallpaper = wallpaper;
            this.viewModel.OnWindowCloseRequested += ViewModel_OnWindowCloseRequested;
            this.viewModel.LoadModel(wallpaper.Model.LivelyInfo);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private void ViewModel_OnWindowCloseRequested(object sender, bool success)
        {
            this.DialogResult = success;
            this.Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Update capture parameters
            viewModel.CapturePosition = PreviewBorder.GetAbsolutePlacement(true);
            viewModel.CaptureArea = PreviewBorder.GetElementPixelSize();

            // Attach wp hwnd to border ui element.
            this.SetProgramToFramework(viewModel.Wallpaper.Handle, PreviewBorder);
            // Refocus window to allow keyboard input.
            this.Activate();

            // Subscribing after Window loaded for framework elements to be initialized.
            this.LocationChanged += Window_LocationChanged;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.IsProcessing)
            {
                e.Cancel = true;
                return;
            }

            // Detach wallpaper window from this dialogue.
            WindowUtil.TrySetParent(viewModel.Wallpaper.Handle, IntPtr.Zero);
            // Move outside visibile region.
            NativeMethods.SetWindowPos(viewModel.Wallpaper.Handle, 1, -9999, 0, 0, 0, 0x0002);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // To clean up resources if any.
                viewModel.CancelCommand.Execute(null);
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            // Update capture parameters
            // Note: Framework elements needs to be initialized for these calls.
            viewModel.CapturePosition = PreviewBorder.GetAbsolutePlacement(true);
            viewModel.CaptureArea = PreviewBorder.GetElementPixelSize();
        }

        // Prevent window resize and move during recording.
        // Ref: https://stackoverflow.com/questions/3419909/how-do-i-lock-a-wpf-window-so-it-can-not-be-moved-resized-minimized-maximized
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING && viewModel.IsProcessing)
            {
                var wp = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
                wp.flags |= (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE;
                Marshal.StructureToPtr(wp, lParam, false);
            }
            return IntPtr.Zero;
        }
    }
}
