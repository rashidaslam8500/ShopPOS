using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ShopPOS.WPF.Converters;

/// <summary>
/// Two-way converter for nullable decimal amount inputs — blank when null, whole numbers only.
/// </summary>
public class NullableDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        if (value is decimal d)
            return Math.Round(d, 0, MidpointRounding.AwayFromZero).ToString("0", culture);

        return string.Empty;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
            return null;

        if (decimal.TryParse(text, NumberStyles.Number, culture, out var parsed)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            return Math.Round(parsed, 0, MidpointRounding.AwayFromZero);

        return DependencyProperty.UnsetValue;
    }
}
