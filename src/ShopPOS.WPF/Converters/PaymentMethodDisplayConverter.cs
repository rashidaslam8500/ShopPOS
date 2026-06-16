using System.Globalization;
using System.Windows.Data;
using ShopPOS.Domain.Enums;

namespace ShopPOS.WPF.Converters;

public class PaymentMethodDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PaymentMethod method ? method.ToDisplayName() : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
