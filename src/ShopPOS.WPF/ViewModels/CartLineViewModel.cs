using CommunityToolkit.Mvvm.ComponentModel;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class CartLineViewModel : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private string _skuOrBarcode = string.Empty;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private string _unitPriceText = string.Empty;
    [ObservableProperty] private string _lineTotalText = string.Empty;

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
        UpdateLineTotalText();
    }

    partial void OnUnitPriceChanged(decimal value) => UpdateLineTotalText();

    private void UpdateLineTotalText()
    {
        var total = Math.Round(LineTotal, 0, MidpointRounding.AwayFromZero);
        LineTotalText = $"Rs. {total:N0}";
    }

    public CartItem ToModel() => new()
    {
        ProductId = ProductId,
        Name = Name,
        UnitPrice = UnitPrice,
        Quantity = Quantity
    };

    public static CartLineViewModel FromProduct(Domain.Entities.Product product)
    {
        var price = Math.Round(product.Price, 0, MidpointRounding.AwayFromZero);
        return new()
        {
            ProductId = product.Id,
            Name = product.Name,
            Category = product.Category,
            SkuOrBarcode = !string.IsNullOrWhiteSpace(product.Sku)
                ? product.Sku
                : product.Barcode ?? "—",
            UnitPrice = product.Price,
            Quantity = 1,
            UnitPriceText = $"Rs. {price:N0}",
            LineTotalText = $"Rs. {price:N0}"
        };
    }
}
