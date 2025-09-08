using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors
{
    public static class CheckBoxCheckedChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(CheckBoxCheckedChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(CheckBoxCheckedChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(CheckBox checkBox)
        {
            return (ICommand)checkBox.GetValue(CommandProperty);
        }

        public static void SetCommand(CheckBox checkBox, ICommand value)
        {
            checkBox.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(CheckBox checkBox)
        {
            return checkBox.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(CheckBox checkBox, object value)
        {
            checkBox.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CheckBox checkBox)
                return;

            checkBox.Checked -= OnCheckBoxCheckedChanged;
            checkBox.Unchecked -= OnCheckBoxCheckedChanged;

            if (e.NewValue is ICommand)
            {
                checkBox.Checked += OnCheckBoxCheckedChanged;
                checkBox.Unchecked += OnCheckBoxCheckedChanged;
            }
        }

        private static void OnCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var command = GetCommand(checkBox);
                var parameter = GetCommandParameter(checkBox);
                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
