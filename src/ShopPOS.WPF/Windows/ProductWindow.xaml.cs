using System.Windows;
using System.Windows.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;

namespace ShopPOS.WPF.Windows;

public partial class ProductWindow : Window
{
    private readonly IProductService? _products;
    public Product? Product { get; private set; }

    public ProductWindow(Product? existing = null, IProductService? products = null)
    {
        _products = products;
        InitializeComponent();
        Loaded += (_, _) => BarcodeBox.Focus();

        if (existing is not null)
        {
            Product = new Product
            {
                Id = existing.Id,
                Name = existing.Name,
                Category = existing.Category,
                Description = existing.Description,
                Price = existing.Price,
                Stock = existing.Stock,
                Barcode = existing.Barcode,
                CreatedAt = existing.CreatedAt
            };
            NameBox.Text = Product.Name;
            CategoryBox.Text = Product.Category;
            DescriptionBox.Text = Product.Description ?? string.Empty;
            PriceBox.Text = Product.Price.ToString("0.##");
            StockBox.Text = Product.Stock.ToString();
            BarcodeBox.Text = Product.Barcode ?? string.Empty;
            Title = "Edit Product";
        }
    }

    public void LoadProduct(Product existing)
    {
        Product = new Product
        {
            Id = existing.Id,
            Name = existing.Name,
            Category = existing.Category,
            Description = existing.Description,
            Price = existing.Price,
            Stock = existing.Stock,
            Barcode = existing.Barcode,
            Sku = existing.Sku,
            CreatedAt = existing.CreatedAt
        };
        NameBox.Text = Product.Name;
        CategoryBox.Text = Product.Category;
        DescriptionBox.Text = Product.Description ?? string.Empty;
        PriceBox.Text = Product.Price.ToString("0.##");
        StockBox.Text = Product.Stock.ToString();
        BarcodeBox.Text = Product.Barcode ?? string.Empty;
        Title = "Edit Product";
    }

    private async void AutoGenerateBarcode_Click(object sender, RoutedEventArgs e)
    {
        if (_products is null)
        {
            MessageBox.Show("Barcode service unavailable.", "Product");
            return;
        }

        try
        {
            BarcodeBox.Text = await _products.GenerateUniqueBarcodeAsync(Product?.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not generate barcode.\n\n{ex.Message}", "Product");
        }
    }

    private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            e.Handled = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(PriceBox.Text, out var price) || !int.TryParse(StockBox.Text, out var stock))
        {
            MessageBox.Show("Enter valid price and stock.", "Validation");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Product name is required.", "Validation");
            return;
        }

        Product ??= new Product();
        Product.Name = NameBox.Text.Trim();
        Product.Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? "General" : CategoryBox.Text.Trim();
        Product.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
        Product.Price = price;
        Product.Stock = stock;
        Product.Barcode = string.IsNullOrWhiteSpace(BarcodeBox.Text) ? null : BarcodeBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
