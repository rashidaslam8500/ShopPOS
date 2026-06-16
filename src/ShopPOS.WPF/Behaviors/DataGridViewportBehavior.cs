using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ShopPOS.WPF.Behaviors;

public static class DataGridViewportBehavior
{
    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(DataGridViewportBehavior));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridViewportBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if ((bool)e.NewValue)
        {
            grid.Loaded += OnGridLoaded;
            grid.Unloaded += OnGridUnloaded;
            grid.SizeChanged += OnGridSizeChanged;
            grid.ColumnReordered += OnGridColumnsChanged;
            grid.ColumnDisplayIndexChanged += OnGridColumnsChanged;

            if (grid.IsLoaded)
                Refresh(grid);
        }
        else
        {
            grid.Loaded -= OnGridLoaded;
            grid.Unloaded -= OnGridUnloaded;
            grid.SizeChanged -= OnGridSizeChanged;
            grid.ColumnReordered -= OnGridColumnsChanged;
            grid.ColumnDisplayIndexChanged -= OnGridColumnsChanged;
        }
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs e) => Refresh((DataGrid)sender);

    private static void OnGridUnloaded(object sender, RoutedEventArgs e) { }

    private static void OnGridSizeChanged(object sender, SizeChangedEventArgs e) => Refresh((DataGrid)sender);

    private static void OnGridColumnsChanged(object? sender, DataGridColumnEventArgs e) =>
        Refresh((DataGrid)sender!);

    private static void Refresh(DataGrid grid)
    {
        if ((bool)grid.GetValue(IsUpdatingProperty))
            return;

        if (!grid.IsLoaded || grid.ActualWidth <= 0)
            return;

        grid.SetValue(IsUpdatingProperty, true);
        try
        {
            ConfigureLastColumnGripper(grid);
            EnforceViewport(grid);
        }
        finally
        {
            grid.SetValue(IsUpdatingProperty, false);
        }
    }

    private static void EnforceViewport(DataGrid grid)
    {
        if (grid.ActualWidth <= 0)
            return;

        var columns = grid.Columns
            .Where(c => c.Visibility == Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        if (columns.Count == 0)
            return;

        var available = GetAvailableWidth(grid);
        var last = columns[^1];
        var lastMin = last.MinWidth > 0 ? last.MinWidth : 48;
        var maxFixed = Math.Max(0, available - lastMin);
        var fixedTotal = columns.Take(columns.Count - 1).Sum(c => c.ActualWidth);

        if (fixedTotal > maxFixed + 0.5)
        {
            var excess = fixedTotal - maxFixed;
            for (var i = columns.Count - 2; i >= 0 && excess > 0.5; i--)
            {
                var column = columns[i];
                var min = column.MinWidth > 0 ? column.MinWidth : 40;
                var shrinkable = column.ActualWidth - min;
                if (shrinkable <= 0)
                    continue;

                var shrink = Math.Min(shrinkable, excess);
                column.Width = new DataGridLength(column.ActualWidth - shrink);
                excess -= shrink;
            }
        }
    }

    private static double GetAvailableWidth(DataGrid grid)
    {
        var width = grid.ActualWidth;

        if (grid.RowHeaderWidth > 0)
            width -= grid.RowHeaderWidth;

        var scrollViewer = FindVisualChild<ScrollViewer>(grid);
        if (scrollViewer?.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            width -= SystemParameters.VerticalScrollBarWidth;

        return Math.Max(0, width);
    }

    private static void ConfigureLastColumnGripper(DataGrid grid)
    {
        var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(grid);
        if (headersPresenter is null)
            return;

        var headers = new List<DataGridColumnHeader>();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(headersPresenter); i++)
        {
            if (VisualTreeHelper.GetChild(headersPresenter, i) is DataGridColumnHeader header
                && header.Visibility == Visibility.Visible
                && header.ActualWidth > 0)
            {
                headers.Add(header);
            }
        }

        if (headers.Count == 0)
            return;

        var lastDisplayIndex = headers.Max(h => h.DisplayIndex);
        foreach (var header in headers)
        {
            var isLast = header.DisplayIndex == lastDisplayIndex;
            SetGripperVisibility(header, "PART_RightHeaderGripper", isLast ? Visibility.Collapsed : Visibility.Visible);
            SetGripperVisibility(header, "PART_LeftHeaderGripper", Visibility.Visible);
        }
    }

    private static void SetGripperVisibility(DataGridColumnHeader header, string partName, Visibility visibility)
    {
        if (header.Template?.FindName(partName, header) is UIElement gripper)
            gripper.Visibility = visibility;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
