using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Enums;
using ShopPOS.WPF.Services;

namespace ShopPOS.WPF.Windows;

public partial class VendorPurchaseEntryWindow : Window
{
    private readonly IVendorKhataService _khata;
    private readonly IVendorBillStorageService _billStorage;
    private int _vendorId;
    private string? _pendingAttachmentSourcePath;

    public VendorPurchaseEntryWindow(IVendorKhataService khata, IVendorBillStorageService billStorage)
    {
        InitializeComponent();
        _khata = khata;
        _billStorage = billStorage;
        PaymentModeBox.ItemsSource = Enum.GetValues(typeof(VendorKhataPaymentMode));
        PaymentModeBox.SelectedItem = VendorKhataPaymentMode.Cash;
        DatePicker.SelectedDate = DateTime.Today;
    }

    public void Initialize(int vendorId) => _vendorId = vendorId;

    private void AddHardcopy_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Bill Hardcopy",
            Filter = "Bill Files|*.jpg;*.jpeg;*.png;*.pdf|Image Files|*.jpg;*.jpeg;*.png|PDF Documents|*.pdf",
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;

        _pendingAttachmentSourcePath = dialog.FileName;
        AttachmentStatusText.Text = $"Selected: {Path.GetFileName(dialog.FileName)}";
        AttachmentStatusText.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!DatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a transaction date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TotalBillBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var totalBill)
            && !decimal.TryParse(TotalBillBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out totalBill))
            totalBill = 0;

        if (!decimal.TryParse(CashPaidBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var cashPaid)
            && !decimal.TryParse(CashPaidBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out cashPaid))
            cashPaid = 0;

        totalBill = Math.Round(totalBill, 0, MidpointRounding.AwayFromZero);
        cashPaid = Math.Round(cashPaid, 0, MidpointRounding.AwayFromZero);

        var paymentMode = PaymentModeBox.SelectedItem is VendorKhataPaymentMode mode
            ? mode
            : VendorKhataPaymentMode.Cash;

        var description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

        try
        {
            IsEnabled = false;
            var entryId = await _khata.AddKhataEntryReturnIdAsync(
                _vendorId,
                DatePicker.SelectedDate.Value,
                InvoiceBox.Text.Trim(),
                totalBill,
                cashPaid,
                paymentMode,
                description);

            if (!string.IsNullOrWhiteSpace(_pendingAttachmentSourcePath))
            {
                try
                {
                    var savedPath = await _billStorage.ImportBillAsync(_vendorId, entryId, _pendingAttachmentSourcePath);
                    await _khata.SetAttachmentPathAsync(entryId, savedPath);
                }
                catch
                {
                    await _khata.SoftDeleteKhataEntryAsync(entryId);
                    throw;
                }
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Save Invoice", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
