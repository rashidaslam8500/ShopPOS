using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class ReturnLineViewModel : ObservableObject
{
    public int SaleItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public int RemainingQty { get; set; }

    [ObservableProperty] private int _returnQty;
    [ObservableProperty] private string? _reason;

    public string OriginalPriceText => $"Rs. {Math.Round(OriginalPrice, 0, MidpointRounding.AwayFromZero):N0} (invoice rate)";
}

public partial class AmendmentAddLineViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int AvailableStock { get; set; }

    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private string? _reason;

    public string UnitPriceText => $"Rs. {Math.Round(UnitPrice, 0, MidpointRounding.AwayFromZero):N0} (current rate)";
    public decimal LineTotal => UnitPrice * Quantity;
    public string LineTotalText => $"Rs. {Math.Round(LineTotal, 0, MidpointRounding.AwayFromZero):N0}";

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
        OnPropertyChanged(nameof(LineTotalText));
    }
}

public partial class ReturnsViewModel : ViewModelBase
{
    private readonly ISaleService _sales;
    private readonly IProductService _products;
    private readonly ISettingsService _settings;
    private readonly IReceiptPrintService _printer;
    private Sale? _loadedSale;

    [ObservableProperty] private string _receiptSearch = string.Empty;
    [ObservableProperty] private string _invoiceInfo = "Search an invoice to process returns or additions.";
    [ObservableProperty] private string _productSearch = string.Empty;
    [ObservableProperty] private string _netTotalText = string.Empty;

    public ObservableCollection<ReturnLineViewModel> Lines { get; } = new();
    public ObservableCollection<AmendmentAddLineViewModel> Additions { get; } = new();
    public ObservableCollection<Product> ProductSuggestions { get; } = new();

    public ReturnsViewModel(
        ISaleService sales,
        IProductService products,
        ISettingsService settings,
        IReceiptPrintService printer)
    {
        _sales = sales;
        _products = products;
        _settings = settings;
        _printer = printer;
    }

    partial void OnProductSearchChanged(string value) => _ = SearchProductsAsync(value);

    [RelayCommand]
    private async Task LoadInvoiceAsync()
    {
        await RunSafeAsync(async () =>
        {
            Lines.Clear();
            Additions.Clear();
            ProductSuggestions.Clear();
            ProductSearch = string.Empty;

            _loadedSale = await _sales.GetByReceiptNoAsync(InvoiceScanHelper.Normalize(ReceiptSearch));
            if (_loadedSale is null)
            {
                InvoiceInfo = "Invoice not found.";
                NetTotalText = string.Empty;
                return;
            }

            ReceiptSearch = _loadedSale.ReceiptNo;
            UpdateInvoiceSummary(_loadedSale);
            foreach (var item in _loadedSale.Items.Where(i => !i.IsAmendmentLine && i.RemainingQuantity > 0))
            {
                Lines.Add(new ReturnLineViewModel
                {
                    SaleItemId = item.Id,
                    ProductName = item.ProductName,
                    OriginalPrice = item.UnitPriceAtSale,
                    RemainingQty = item.RemainingQuantity
                });
            }
        });
    }

    public async Task LoadFromScannerAsync(string scannedValue)
    {
        ReceiptSearch = InvoiceScanHelper.Normalize(scannedValue);
        await LoadInvoiceAsync();
    }

    [RelayCommand]
    private async Task SearchProductsAsync(string? query)
    {
        var term = query?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            ProductSuggestions.Clear();
            return;
        }

        try
        {
            var items = await _products.GetProductsAsync(term);
            ProductSuggestions.Clear();
            foreach (var product in items.Take(8))
                ProductSuggestions.Add(product);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void AddProductToAmendments(Product? product)
    {
        if (product is null || product.Stock <= 0)
            return;

        var existing = Additions.FirstOrDefault(a => a.ProductId == product.Id);
        if (existing is not null)
        {
            if (existing.Quantity < product.Stock)
                existing.Quantity++;
            return;
        }

        Additions.Add(new AmendmentAddLineViewModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            AvailableStock = product.Stock,
            Quantity = 1
        });

        ProductSearch = string.Empty;
        ProductSuggestions.Clear();
    }

    [RelayCommand]
    private void RemoveAddition(AmendmentAddLineViewModel? line)
    {
        if (line is not null)
            Additions.Remove(line);
    }

    [RelayCommand]
    private async Task CorrectAndProceedAsync()
    {
        if (_loadedSale is null) return;

        var returnRequests = Lines
            .Where(l => l.ReturnQty > 0)
            .Select(l => new ReturnRequest { SaleItemId = l.SaleItemId, Quantity = l.ReturnQty, Reason = l.Reason })
            .ToList();

        var addRequests = Additions
            .Where(a => a.Quantity > 0)
            .Select(a => new AddItemRequest { ProductId = a.ProductId, Quantity = a.Quantity, Reason = a.Reason })
            .ToList();

        if (returnRequests.Count == 0 && addRequests.Count == 0)
        {
            System.Windows.MessageBox.Show("Enter return quantities and/or add new items.", "Bhai Gee POS");
            return;
        }

        await RunSafeAsync(async () =>
        {
            var sale = await _sales.ProcessCorrectAndProceedAsync(_loadedSale.Id, returnRequests, addRequests);
            var config = await _settings.GetConfigAsync();

            try { _printer.PrintReceipt(sale, config); }
            catch { /* printer optional */ }

            System.Windows.MessageBox.Show(
                _printer.BuildReceiptText(sale, config),
                $"Updated Receipt — {sale.ReceiptNo}");

            ReceiptSearch = sale.ReceiptNo;
            await LoadInvoiceAsync();
        });
    }

    private void UpdateInvoiceSummary(Sale sale)
    {
        var net = sale.NetTotal > 0 || sale.ReturnedAmount > 0 || sale.AddedAmount > 0
            ? sale.NetTotal
            : sale.Total - sale.ReturnedAmount + sale.AddedAmount;

        InvoiceInfo = $"{sale.ReceiptNo} — {sale.SaleDate:g} — Original {_settings.FormatMoney(sale.Total)} — Net {_settings.FormatMoney(net)}";
        NetTotalText = $"Current net total: {_settings.FormatMoney(net)}";
    }
}
