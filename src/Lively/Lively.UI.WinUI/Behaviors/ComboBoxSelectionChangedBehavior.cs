using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors
{
    public static class ComboBoxSelectionChangedBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ComboBoxSelectionChangedBehavior), new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(ComboBoxSelectionChangedBehavior), new PropertyMetadata(null));

        public static ICommand GetCommand(ComboBox comboBox)
        {
            return (ICommand)comboBox.GetValue(CommandProperty);
        }

        public static void SetCommand(ComboBox comboBox, ICommand value)
        {
            comboBox.SetValue(CommandProperty, value);
        }

        public static object GetCommandParameter(ComboBox comboBox)
        {
            return comboBox.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(ComboBox comboBox, object value)
        {
            comboBox.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox comboBox)
                return;

            comboBox.SelectionChanged -= OnComboBoxSelectionChanged;

            if (e.NewValue is ICommand)
            {
                comboBox.SelectionChanged += OnComboBoxSelectionChanged;
            }
        }

        private static void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var command = GetCommand(comboBox);
                var parameter = GetCommandParameter(comboBox);
                if (command?.CanExecute(parameter) == true)
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
