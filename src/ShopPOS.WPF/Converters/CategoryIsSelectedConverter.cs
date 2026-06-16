using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShopPOS.WPF.Converters;

public class CategoryIsSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;

        var category = values[0]?.ToString() ?? string.Empty;
        var selected = values[1]?.ToString() ?? string.Empty;
        return string.Equals(category, selected, StringComparison.Ordinal);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class BoolToStyleConverter : IValueConverter
{
    public Style? ActiveStyle { get; set; }
    public Style? InactiveStyle { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? ActiveStyle : InactiveStyle;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
