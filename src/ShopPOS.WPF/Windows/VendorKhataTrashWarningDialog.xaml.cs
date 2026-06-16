using System.Windows;

namespace ShopPOS.WPF.Windows;

public partial class VendorKhataTrashWarningDialog : Window
{
    public VendorKhataTrashWarningDialog()
    {
        InitializeComponent();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
