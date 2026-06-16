using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;

namespace ShopPOS.WPF.ViewModels;

public partial class SalesViewModel : ViewModelBase
{
    private readonly ISaleService _sales;
    private readonly ISettingsService _settings;
    private readonly IReceiptPrintService _printer;
    private readonly IAppShell _shell;

    [ObservableProperty] private DateTime? _filterDate;
    [ObservableProperty] private string _invoiceScanInput = string.Empty;

    public ObservableCollection<Sale> Items { get; } = new();

    public SalesViewModel(ISaleService sales, ISettingsService settings, IReceiptPrintService printer, IAppShell shell)
    {
        _sales = sales;
        _settings = settings;
        _printer = printer;
        _shell = shell;
        _ = LoadAsync();
    }

    partial void OnFilterDateChanged(DateTime? value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _sales.GetSalesAsync(FilterDate);
            Items.Clear();
            foreach (var s in list) Items.Add(s);
        });
    }

    [RelayCommand]
    private async Task ViewReceiptAsync(Sale? sale)
    {
        if (sale is null) return;
        var full = await _sales.GetSaleAsync(sale.Id) ?? sale;
        var config = await _settings.GetConfigAsync();
        System.Windows.MessageBox.Show(_printer.BuildReceiptText(full, config), full.ReceiptNo);
    }

    [RelayCommand]
    private async Task PrintReceiptAsync(Sale? sale)
    {
        if (sale is null) return;
        var full = await _sales.GetSaleAsync(sale.Id) ?? sale;
        var config = await _settings.GetConfigAsync();
        try { _printer.PrintReceipt(full, config); }
        catch (Exception ex) { System.Windows.MessageBox.Show($"Print failed: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ScanInvoiceAsync()
    {
        if (string.IsNullOrWhiteSpace(InvoiceScanInput))
            return;

        await _shell.OpenReturnsForInvoiceAsync(InvoiceScanInput);
        InvoiceScanInput = string.Empty;
    }

    [RelayCommand]
    private async Task MoveToTrashAsync(Sale? sale)
    {
        if (sale is null) return;
        if (System.Windows.MessageBox.Show($"Move invoice {sale.ReceiptNo} to trash?", "Confirm",
                System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
            return;

        await RunSafeAsync(async () =>
        {
            await _sales.SoftDeleteSaleAsync(sale.Id);
            await LoadAsync();
        }, "Invoice moved to trash.");
    }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);
}
