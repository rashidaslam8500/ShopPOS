using ShopPOS.Business.Services;

using ShopPOS.Domain.Entities;

using ShopPOS.Domain.Models;



namespace ShopPOS.WPF.ViewModels;



public class SearchSuggestionViewModel

{

    public Product Product { get; }



    public string Name => Product.Name;

    public string Category => Product.Category;

    public string DetailText { get; }

    public string ReferenceText { get; }

    public string StockText => Product.Stock > 0 ? $"{Product.Stock} pcs" : "Out of stock";



    public SearchSuggestionViewModel(Product product, ISettingsService settings, ShopConfig config)

    {

        Product = product;

        DetailText = $"{settings.FormatMoney(product.Price, config)}  •  {StockText}";

        var refs = new List<string>();

        if (!string.IsNullOrWhiteSpace(product.Barcode))

            refs.Add($"Barcode: {product.Barcode}");

        if (!string.IsNullOrWhiteSpace(product.Sku))

            refs.Add($"Ref: {product.Sku}");

        ReferenceText = refs.Count > 0 ? string.Join("  •  ", refs) : "—";

    }

}

