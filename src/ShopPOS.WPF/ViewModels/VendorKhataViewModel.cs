using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;
using ShopPOS.WPF.Helpers;
using ShopPOS.WPF.Services;
using ShopPOS.WPF.Services.Reports;
using ShopPOS.WPF.Windows;

namespace ShopPOS.WPF.ViewModels;

public partial class VendorKhataViewModel : ViewModelBase
{
    private readonly IVendorKhataService _khata;
    private readonly ISettingsService _settings;
    private readonly IVendorBillStorageService _billStorage;
    private readonly ILedgerPdfReportService _pdfReports;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty] private string _newVendorName = string.Empty;
    [ObservableProperty] private string _newVendorPhone = string.Empty;
    [ObservableProperty] private string _newVendorAddress = string.Empty;
    [ObservableProperty] private Vendor? _selectedVendor;

    [ObservableProperty] private VendorKhataLine? _selectedKhataLine;
    [ObservableProperty] private VendorKhataTrashItemViewModel? _selectedTrashItem;
    [ObservableProperty] private bool _isTrashSectionVisible;

    [ObservableProperty] private string _totalDuesText = "Rs. 0";
    [ObservableProperty] private string _totalPaidText = "Rs. 0";
    [ObservableProperty] private string _netBalanceText = "Rs. 0";

    public ObservableCollection<Vendor> Vendors { get; } = new();
    public ObservableCollection<VendorKhataLine> KhataLines { get; } = new();
    public ObservableCollection<VendorKhataTrashItemViewModel> TrashItems { get; } = new();

    public VendorKhataViewModel(
        IVendorKhataService khata,
        ISettingsService settings,
        IVendorBillStorageService billStorage,
        ILedgerPdfReportService pdfReports,
        IServiceScopeFactory scopeFactory)
    {
        _khata = khata;
        _settings = settings;
        _billStorage = billStorage;
        _pdfReports = pdfReports;
        _scopeFactory = scopeFactory;
        _ = LoadVendorsAsync();
    }

    partial void OnSelectedVendorChanged(Vendor? value) => _ = LoadKhataAsync();

    [RelayCommand]
    private async Task LoadVendorsAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _khata.GetVendorsAsync();
            Vendors.Clear();
            foreach (var v in list) Vendors.Add(v);
        });
    }

    [RelayCommand]
    private async Task AddVendorAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVendorName)) return;
        await RunSafeAsync(async () =>
        {
            await _khata.AddVendorAsync(NewVendorName, NewVendorPhone, NewVendorAddress);
            NewVendorName = string.Empty;
            NewVendorPhone = string.Empty;
            NewVendorAddress = string.Empty;
            await LoadVendorsAsync();
        }, "Vendor added successfully.");
    }

    [RelayCommand]
    private async Task UpdateVendorProfileAsync()
    {
        if (SelectedVendor is null)
        {
            System.Windows.MessageBox.Show(
                "Please select a vendor before updating the profile.",
                "Vendor Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var vendorId = SelectedVendor.Id;

        if (WindowNavigationHelper.ActivateIfOpen<VendorProfileUpdateWindow>(existing => existing.LoadFrom(SelectedVendor)))
            return;

        var window = CreateScopedWindow<VendorProfileUpdateWindow>();
        window.LoadFrom(SelectedVendor);
        window.Owner = System.Windows.Application.Current.MainWindow;
        if (window.ShowDialog() != true)
            return;

        await RunSafeAsync(async () =>
        {
            await _khata.UpdateVendorAsync(
                vendorId,
                window.Form.VendorName,
                window.Form.Phone,
                string.IsNullOrWhiteSpace(window.Form.Address) ? null : window.Form.Address.Trim());
            await LoadVendorsAsync();
            SelectedVendor = Vendors.FirstOrDefault(v => v.Id == vendorId);
            System.Windows.MessageBox.Show(
                "Vendor profile has been saved successfully.",
                "Profile Updated",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        });
    }

    [RelayCommand]
    private async Task LoadKhataAsync()
    {
        if (SelectedVendor is null)
        {
            KhataLines.Clear();
            TrashItems.Clear();
            TotalDuesText = TotalPaidText = NetBalanceText = _settings.FormatMoney(0);
            SelectedKhataLine = null;
            return;
        }

        await RefreshLedgerAsync();
    }

    private async Task RefreshLedgerAsync()
    {
        if (SelectedVendor is null) return;

        var summary = await _khata.GetSummaryAsync(SelectedVendor.Id);
        TotalDuesText = _settings.FormatMoney(summary.TotalDues);
        TotalPaidText = _settings.FormatMoney(summary.TotalCashPaid);
        NetBalanceText = _settings.FormatMoney(summary.CurrentNetBalance);

        var lines = await _khata.GetKhataLinesAsync(SelectedVendor.Id);
        KhataLines.Clear();
        foreach (var line in lines)
        {
            line.HasAttachment = _billStorage.FileExists(line.AttachmentPath);
            KhataLines.Add(line);
        }

        var trash = await _khata.GetTrashedLinesAsync(SelectedVendor.Id);
        TrashItems.Clear();
        foreach (var line in trash)
        {
            var item = new VendorKhataTrashItemViewModel(line);
            item.PropertyChanged += OnTrashItemPropertyChanged;
            TrashItems.Add(item);
        }
        IsTrashSectionVisible = TrashItems.Count > 0;
    }

    [RelayCommand]
    private async Task PrintLedgerPdfAsync()
    {
        if (SelectedVendor is null)
        {
            System.Windows.MessageBox.Show(
                "Please select a vendor before printing the ledger report.",
                "Vendor Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var safeName = SelectedVendor.Name.Replace(' ', '_');
        var path = PdfReportExportHelper.PromptSavePath($"VendorLedger_{safeName}_{DateTime.Now:yyyyMMdd}.pdf");
        if (path is null)
            return;

        await RunSafeAsync(async () =>
        {
            var config = await _settings.GetConfigAsync();
            var summary = await _khata.GetSummaryAsync(SelectedVendor.Id);
            var lines = await _khata.GetKhataLinesAsync(SelectedVendor.Id);

            var report = new VendorLedgerReportData
            {
                ShopTitle = BrandDisplay.TitleLine,
                ShopContactLine = $"{config.Address}  |  {config.Phone}",
                ExportDate = DateTime.Now,
                VendorName = SelectedVendor.Name,
                VendorPhone = SelectedVendor.Phone,
                VendorAddress = SelectedVendor.Address ?? string.Empty,
                TotalBillsText = PdfReportFormatting.FormatAmount(summary.TotalDues),
                TotalPaidText = PdfReportFormatting.FormatAmount(summary.TotalCashPaid),
                RemainingBalanceText = PdfReportFormatting.FormatAmount(summary.CurrentNetBalance),
                Lines = lines.Select(line => new VendorLedgerReportLine
                {
                    DateText = line.Date.ToString("dd/MM/yyyy"),
                    InvoiceNumber = string.IsNullOrWhiteSpace(line.InvoiceNumber) ? "—" : line.InvoiceNumber,
                    Description = string.IsNullOrWhiteSpace(line.Description) ? "—" : line.Description!,
                    PreviousBalanceText = PdfReportFormatting.FormatAmount(line.PreviousBalance),
                    TotalBillText = PdfReportFormatting.FormatAmount(line.TotalBill),
                    CashPaidText = PdfReportFormatting.FormatAmount(line.CashPaid),
                    RemainingBalanceText = PdfReportFormatting.FormatAmount(line.RunningBalance)
                }).ToList()
            };

            _pdfReports.GenerateVendorLedger(report, path);

            if (PdfReportExportHelper.ConfirmOpenAfterSave())
                PdfReportExportHelper.OpenPdf(path);
        });
    }

    [RelayCommand]
    private async Task NewPurchaseEntryAsync()
    {
        if (SelectedVendor is null)
        {
            System.Windows.MessageBox.Show(
                "Please select a vendor before creating a purchase entry.",
                "Vendor Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        if (WindowNavigationHelper.ActivateIfOpen<VendorPurchaseEntryWindow>(existing => existing.Initialize(SelectedVendor.Id)))
            return;

        var window = CreateScopedWindow<VendorPurchaseEntryWindow>();
        window.Initialize(SelectedVendor.Id);
        window.Owner = System.Windows.Application.Current.MainWindow;
        if (window.ShowDialog() == true)
            await RefreshLedgerAsync();
    }

    [RelayCommand]
    private void EditEntry()
    {
        if (SelectedKhataLine is null) return;

        if (WindowNavigationHelper.ActivateIfOpen<VendorKhataEntryEditDialog>(existing => existing.LoadFrom(SelectedKhataLine)))
            return;

        var dialog = new VendorKhataEntryEditDialog
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        dialog.LoadFrom(SelectedKhataLine);
        if (dialog.ShowDialog() != true) return;

        _ = RunSafeAsync(async () =>
        {
            await _khata.UpdateKhataEntryAsync(
                SelectedKhataLine.Id,
                dialog.EntryDate,
                dialog.InvoiceNumber,
                dialog.TotalBill,
                dialog.CashPaid,
                dialog.PaymentMode,
                dialog.Notes);
            await RefreshLedgerAsync();
        }, "Entry updated. Balances recalculated.");
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedKhataLine is null) return;

        var dialog = new VendorKhataTrashWarningDialog
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        var entryId = SelectedKhataLine.Id;
        SelectedKhataLine = null;

        _ = RunSafeAsync(async () =>
        {
            await _khata.SoftDeleteKhataEntryAsync(entryId);
            await RefreshLedgerAsync();
        }, "Entry moved to Vendor Trash. Balances updated.");
    }

    [RelayCommand]
    private void ViewBill(VendorKhataLine? line)
    {
        if (line is null || !_billStorage.FileExists(line.AttachmentPath)) return;

        if (WindowNavigationHelper.ActivateIfOpen<VendorBillViewerWindow>(existing => existing.LoadAttachment(line.AttachmentPath!)))
            return;

        var viewer = CreateScopedWindow<VendorBillViewerWindow>();
        viewer.Owner = System.Windows.Application.Current.MainWindow;
        viewer.LoadAttachment(line.AttachmentPath!);
        viewer.ShowDialog();
    }

    [RelayCommand]
    private async Task RestoreEntryAsync()
    {
        var ids = GetSelectedTrashIds(includeFocusedRow: true);
        if (ids.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "Please select at least one trash entry using the checkboxes or row selection.",
                "Nothing Selected",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        if (SelectedVendor is null) return;

        await RunSafeAsync(async () =>
        {
            foreach (var id in ids)
                await _khata.RestoreKhataEntryAsync(id);
            SelectedTrashItem = null;
            await RefreshLedgerAsync();
        }, "Selected entries restored. Summary cards updated.");
    }

    [RelayCommand]
    private async Task PermanentDeleteTrashAsync()
    {
        var ids = GetSelectedTrashIds(includeFocusedRow: false);
        if (ids.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "Please tick the Select checkbox on at least one trash entry before permanently deleting.",
                "Nothing Selected",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new VendorKhataPermanentDeleteDialog
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        await RunSafeAsync(async () =>
        {
            var attachmentPaths = await _khata.PermanentDeleteKhataEntriesAsync(ids);
            foreach (var path in attachmentPaths)
                _billStorage.TryDeleteFile(path);
            SelectedTrashItem = null;
            await RefreshLedgerAsync();
        }, "Selected entries permanently deleted.");
    }

    private List<int> GetSelectedTrashIds(bool includeFocusedRow)
    {
        var ids = TrashItems.Where(x => x.IsSelected).Select(x => x.Id).ToList();
        if (ids.Count == 0 && includeFocusedRow && SelectedTrashItem is not null)
            ids.Add(SelectedTrashItem.Id);
        return ids;
    }

    partial void OnSelectedTrashItemChanged(VendorKhataTrashItemViewModel? value) { }

    private void OnTrashItemPropertyChanged(object? sender, PropertyChangedEventArgs e) { }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);

    private TWindow CreateScopedWindow<TWindow>() where TWindow : Window
    {
        var scope = _scopeFactory.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<TWindow>();
        window.Closed += (_, _) => scope.Dispose();
        return window;
    }
}
