using CommunityToolkit.Mvvm.ComponentModel;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class ProductTileViewModel : ObservableObject
{
    public Product Product { get; }

    [ObservableProperty] private bool _isHighlighted;

    public string Name => Product.Name;
    public string Category => Product.Category;
    public string PriceText { get; }
    public string StockText => Product.Stock > 0 ? $"{Product.Stock} in stock" : "Out of stock";
    public bool IsOutOfStock => Product.Stock <= 0;

    public ProductTileViewModel(Product product, ISettingsService settings, ShopConfig config)
    {
        Product = product;
        PriceText = settings.FormatMoney(product.Price, config);
    }
}
