using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;

namespace ShopPOS.WPF.ViewModels;

public partial class TrashInvoiceViewModel : ObservableObject
{
    public int SaleId { get; init; }
    public string ReceiptNo { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public decimal Total { get; init; }
    public DateTime? DeletedAt { get; init; }

    [ObservableProperty] private bool _isSelected;

    public static TrashInvoiceViewModel FromSale(Sale sale) => new()
    {
        SaleId = sale.Id,
        ReceiptNo = sale.ReceiptNo,
        SaleDate = sale.SaleDate,
        Total = sale.NetTotal > 0 ? sale.NetTotal : sale.Total,
        DeletedAt = sale.DeletedAt
    };
}

public partial class TrashViewModel : ViewModelBase
{
    private readonly ISaleService _sales;
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _selectAll;

    public ObservableCollection<TrashInvoiceViewModel> Items { get; } = new();

    public TrashViewModel(ISaleService sales, ISettingsService settings)
    {
        _sales = sales;
        _settings = settings;
        _ = LoadAsync();
    }

    partial void OnSelectAllChanged(bool value)
    {
        foreach (var item in Items)
            item.IsSelected = value;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var deleted = await _sales.GetDeletedSalesAsync();
            Items.Clear();
            foreach (var sale in deleted)
                Items.Add(TrashInvoiceViewModel.FromSale(sale));
            SelectAll = false;
        });
    }

    [RelayCommand]
    private async Task BulkRestoreAsync()
    {
        var ids = Items.Where(i => i.IsSelected).Select(i => i.SaleId).ToList();
        if (ids.Count == 0)
        {
            System.Windows.MessageBox.Show("Select at least one invoice.", "Trash Bin");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _sales.RestoreSalesAsync(ids);
            await LoadAsync();
        }, $"{ids.Count} invoice(s) restored.");
    }

    [RelayCommand]
    private async Task BulkPermanentDeleteAsync()
    {
        var ids = Items.Where(i => i.IsSelected).Select(i => i.SaleId).ToList();
        if (ids.Count == 0)
        {
            System.Windows.MessageBox.Show("Select at least one invoice.", "Trash Bin");
            return;
        }

        var dialog = new Windows.OwnerPasswordDialog();
        if (dialog.ShowDialog() != true)
            return;

        await RunSafeAsync(async () =>
        {
            await _sales.PermanentDeleteSalesAsync(ids, dialog.Password);
            await LoadAsync();
        }, $"{ids.Count} invoice(s) permanently deleted.");
    }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);
}
