using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Lively.UI.WinUI.Behaviors;

public static class GridViewItemClickBehavior
{
    public static readonly DependencyProperty ItemClickCommandProperty =
        DependencyProperty.RegisterAttached("ItemClickCommand", typeof(ICommand), typeof(GridViewItemClickBehavior), new PropertyMetadata(null, OnItemClickCommandChanged));

    public static ICommand GetItemClickCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(ItemClickCommandProperty);
    }

    public static void SetItemClickCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(ItemClickCommandProperty, value);
    }

    private static void OnItemClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GridView gridView)
            return;

        gridView.ItemClick -= GridView_ItemClick;

        if (e.NewValue is ICommand)
            gridView.ItemClick += GridView_ItemClick;
    }

    private static void GridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (sender is GridView currentGridView)
        {
            var command = GetItemClickCommand(currentGridView);
            if (command?.CanExecute(e.ClickedItem) == true)
            {
                command.Execute(e.ClickedItem);
            }
        }
    }
}
