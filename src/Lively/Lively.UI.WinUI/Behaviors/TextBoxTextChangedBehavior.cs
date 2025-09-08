using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors
{
    public static class TextBoxTextChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(TextBoxTextChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(TextBoxTextChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(TextBox textBox)
        {
            return (ICommand)textBox.GetValue(CommandProperty);
        }

        public static void SetCommand(TextBox textBox, ICommand value)
        {
            textBox.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(TextBox textBox)
        {
            return textBox.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(TextBox textBox, object value)
        {
            textBox.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
                return;

            textBox.TextChanged -= TextBox_TextChanged;

            if (e.NewValue is ICommand)
                textBox.TextChanged += TextBox_TextChanged;
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var command = GetCommand(textBox);
                var parameter = GetCommandParameter(textBox);
                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
