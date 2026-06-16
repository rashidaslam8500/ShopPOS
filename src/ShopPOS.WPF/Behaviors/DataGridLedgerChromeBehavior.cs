using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShopPOS.WPF.Behaviors;

public static class DataGridLedgerChromeBehavior
{
    private static readonly DependencyProperty AdornerProperty =
        DependencyProperty.RegisterAttached(
            "Adorner",
            typeof(DataGridColumnGuideAdorner),
            typeof(DataGridLedgerChromeBehavior));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridLedgerChromeBehavior),
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
            grid.ColumnDisplayIndexChanged += OnColumnDisplayIndexChanged;

            if (grid.IsLoaded)
                EnsureAdorner(grid);
        }
        else
        {
            grid.Loaded -= OnGridLoaded;
            grid.Unloaded -= OnGridUnloaded;
            grid.SizeChanged -= OnGridSizeChanged;
            grid.ColumnReordered -= OnGridColumnsChanged;
            grid.ColumnDisplayIndexChanged -= OnColumnDisplayIndexChanged;
            RemoveAdorner(grid);
        }
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
            EnsureAdorner(grid);
    }

    private static void OnGridUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
            RemoveAdorner(grid);
    }

    private static void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is DataGrid grid)
            RefreshAdorner(grid);
    }

    private static void OnGridColumnsChanged(object? sender, DataGridColumnEventArgs e)
    {
        if (sender is DataGrid grid)
            RefreshAdorner(grid);
    }

    private static void OnColumnDisplayIndexChanged(object? sender, DataGridColumnEventArgs e)
    {
        if (sender is DataGrid grid)
            RefreshAdorner(grid);
    }

    private static void EnsureAdorner(DataGrid grid)
    {
        grid.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            var layer = AdornerLayer.GetAdornerLayer(grid);
            if (layer is null)
                return;

            if (grid.GetValue(AdornerProperty) is DataGridColumnGuideAdorner existing)
            {
                existing.InvalidateVisual();
                return;
            }

            var adorner = new DataGridColumnGuideAdorner(grid);
            layer.Add(adorner);
            grid.SetValue(AdornerProperty, adorner);
        });
    }

    private static void RemoveAdorner(DataGrid grid)
    {
        if (grid.GetValue(AdornerProperty) is not DataGridColumnGuideAdorner adorner)
            return;

        var layer = AdornerLayer.GetAdornerLayer(grid);
        layer?.Remove(adorner);
        grid.ClearValue(AdornerProperty);
    }

    private static void RefreshAdorner(DataGrid grid)
    {
        if (grid.GetValue(AdornerProperty) is DataGridColumnGuideAdorner adorner)
            adorner.InvalidateVisual();
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

    private sealed class DataGridColumnGuideAdorner : Adorner
    {
        private static readonly Pen GuidePen = CreateGuidePen();

        public DataGridColumnGuideAdorner(DataGrid adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (AdornedElement is not DataGrid grid)
                return;

            var headerHeight = GetHeaderHeight(grid);
            var lastRowBottom = GetLastRowBottom(grid);
            var startY = Math.Max(headerHeight, lastRowBottom);
            var endY = ActualHeight;

            if (endY <= startY + 0.5)
                return;

            foreach (var lineX in GetColumnDividerPositions(grid))
            {
                if (lineX <= 0.5 || lineX >= grid.ActualWidth - 0.5)
                    continue;

                drawingContext.DrawLine(GuidePen, new Point(lineX, startY), new Point(lineX, endY));
            }
        }

        private static IEnumerable<double> GetColumnDividerPositions(DataGrid grid)
        {
            var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(grid);
            if (headersPresenter is null)
                yield break;

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

            foreach (var header in headers.OrderBy(h => h.DisplayIndex))
            {
                var rightEdge = header.TransformToAncestor(grid)
                    .Transform(new Point(header.ActualWidth, 0))
                    .X;
                yield return Math.Round(rightEdge) + 0.5;
            }
        }

        private static double GetLastRowBottom(DataGrid grid)
        {
            var rowsPresenter = FindVisualChild<DataGridRowsPresenter>(grid);
            if (rowsPresenter is null)
                return 0;

            var bottom = 0.0;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(rowsPresenter); i++)
            {
                if (VisualTreeHelper.GetChild(rowsPresenter, i) is not DataGridRow row)
                    continue;

                bottom = Math.Max(bottom, row.TransformToAncestor(grid).Transform(new Point(0, row.ActualHeight)).Y);
            }

            return bottom;
        }

        private static double GetHeaderHeight(DataGrid grid)
        {
            var presenter = FindVisualChild<DataGridColumnHeadersPresenter>(grid);
            return presenter?.ActualHeight ?? 0;
        }

        private static Pen CreateGuidePen()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            brush.Freeze();
            var pen = new Pen(brush, 1);
            pen.Freeze();
            return pen;
        }
    }
}
