using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors
{
    public static class SliderValueChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(SliderValueChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(SliderValueChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(Slider slider)
        {
            return (ICommand)slider.GetValue(CommandProperty);
        }

        public static void SetCommand(Slider slider, ICommand value)
        {
            slider.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(Slider slider)
        {
            return slider.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(Slider slider, object value)
        {
            slider.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Slider slider)
                return;

            slider.ValueChanged -= Slider_ValueChanged;

            if (e.NewValue is ICommand)
                slider.ValueChanged += Slider_ValueChanged;
        }

        private static void Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (sender is Slider slider)
            {
                var command = GetCommand(slider);
                var parameter = GetCommandParameter(slider);
                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
