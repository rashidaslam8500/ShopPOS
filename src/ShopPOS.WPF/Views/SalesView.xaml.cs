using System.Windows.Controls;
using System.Windows.Input;
using ShopPOS.WPF.ViewModels;

namespace ShopPOS.WPF.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
        Loaded += (_, _) => InvoiceScanBox.Focus();
    }

    private SalesViewModel? Vm => DataContext as SalesViewModel;

    private async void InvoiceScanBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Vm is null)
            return;

        e.Handled = true;
        await Vm.ScanInvoiceCommand.ExecuteAsync(null);
    }
}
