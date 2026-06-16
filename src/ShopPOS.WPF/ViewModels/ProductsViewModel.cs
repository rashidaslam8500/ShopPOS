using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.WPF.Helpers;
using ShopPOS.WPF.Windows;

namespace ShopPOS.WPF.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IProductService _products;
    private readonly ISettingsService _settings;
    private readonly IBarcodeService _barcodes;

    [ObservableProperty] private string _searchText = string.Empty;
    public ObservableCollection<Product> Items { get; } = new();

    public ProductsViewModel(IProductService products, ISettingsService settings, IBarcodeService barcodes)
    {
        _products = products;
        _settings = settings;
        _barcodes = barcodes;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _products.GetProductsAsync(SearchText);
            Items.Clear();
            foreach (var p in list) Items.Add(p);
        });
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (WindowNavigationHelper.ActivateIfOpen<ProductWindow>())
            return;

        var dialog = new ProductWindow(products: _products);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.Product is not null)
        {
            await RunSafeAsync(async () =>
            {
                await _products.SaveProductAsync(dialog.Product, isNew: true);
                await LoadAsync();
            }, "Product added.");
        }
    }

    [RelayCommand]
    private async Task EditAsync(Product? product)
    {
        if (product is null) return;

        if (WindowNavigationHelper.ActivateIfOpen<ProductWindow>(existing => existing.LoadProduct(product)))
            return;

        var dialog = new ProductWindow(product, _products);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true && dialog.Product is not null)
        {
            await RunSafeAsync(async () =>
            {
                await _products.SaveProductAsync(dialog.Product, isNew: false);
                await LoadAsync();
            }, "Product updated.");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Product? product)
    {
        if (product is null) return;
        if (System.Windows.MessageBox.Show($"Delete {product.Name}?", "Confirm", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
            return;
        await RunSafeAsync(async () => { await _products.DeleteProductAsync(product.Id); await LoadAsync(); }, "Deleted.");
    }

    [RelayCommand]
    private void ShowBarcode(Product? product)
    {
        if (product?.Barcode is null) return;
        var png = _barcodes.GenerateBarcodePng(product.Barcode);
        var path = Path.Combine(Path.GetTempPath(), $"barcode_{product.Barcode}.png");
        File.WriteAllBytes(path, png);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    public string FormatPrice(decimal price) => _settings.FormatMoney(price);
}
