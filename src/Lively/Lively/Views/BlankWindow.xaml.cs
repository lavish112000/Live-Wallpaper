using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

namespace Lively.Views
{
    /// <summary>
    /// Blank window
    /// </summary>
    public partial class BlankWindow : Window
    {
        public event EventHandler FadeInAnimationCompleted;
        private readonly double fadeInDuration = 0f;
        private readonly double fadeOutDuration = 0f;

        public BlankWindow()
        {
            InitializeComponent();
        }

        public BlankWindow(double fadeInDuration, double fadeOutDuration) : this()
        {
            this.Opacity = 0;
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
            this.fadeInDuration = fadeInDuration;
            this.fadeOutDuration = fadeOutDuration;
            this.Loaded += Window_Loaded;
            this.Closing += Window_Closing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Closing -= Window_Closing;
            e.Cancel = true;
            var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(fadeOutDuration)) {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
            };
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var anim = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromMilliseconds(fadeInDuration)) {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) => FadeInAnimationCompleted?.Invoke(this, EventArgs.Empty);
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}
