using System.Globalization;
using System.Windows;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.Windows;

public partial class VendorKhataEntryEditDialog : Window
{
    public DateTime EntryDate { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal TotalBill { get; private set; }
    public decimal CashPaid { get; private set; }
    public VendorKhataPaymentMode PaymentMode { get; private set; }
    public string? Notes { get; private set; }

    public VendorKhataEntryEditDialog()
    {
        InitializeComponent();
        PaymentModeBox.ItemsSource = Enum.GetValues(typeof(VendorKhataPaymentMode));
    }

    public void LoadFrom(VendorKhataLine line)
    {
        DatePicker.SelectedDate = line.Date;
        InvoiceBox.Text = line.InvoiceNumber;
        TotalBillBox.Text = Math.Round(line.TotalBill, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        CashPaidBox.Text = Math.Round(line.CashPaid, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        PaymentModeBox.SelectedItem = line.PaymentMode;
        NotesBox.Text = line.Description ?? line.Notes ?? string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!DatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Please select a date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TotalBillBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var totalBill))
            totalBill = 0;
        if (!decimal.TryParse(CashPaidBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var cashPaid))
            cashPaid = 0;

        EntryDate = DatePicker.SelectedDate.Value;
        InvoiceNumber = InvoiceBox.Text.Trim();
        TotalBill = Math.Round(totalBill, 0, MidpointRounding.AwayFromZero);
        CashPaid = Math.Round(cashPaid, 0, MidpointRounding.AwayFromZero);
        PaymentMode = PaymentModeBox.SelectedItem is VendorKhataPaymentMode mode
            ? mode
            : VendorKhataPaymentMode.Cash;
        Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
