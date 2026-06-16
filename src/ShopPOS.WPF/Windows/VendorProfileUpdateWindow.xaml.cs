using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using ShopPOS.Domain.Entities;

namespace ShopPOS.WPF.Windows;

public partial class VendorProfileUpdateWindow : Window
{
    public VendorProfileForm Form { get; } = new();

    public VendorProfileUpdateWindow()
    {
        InitializeComponent();
        DataContext = Form;
    }

    public void LoadFrom(Vendor vendor)
    {
        Form.VendorName = vendor.Name;
        Form.Phone = vendor.Phone;
        Form.Address = vendor.Address ?? string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Form.VendorName))
        {
            MessageBox.Show(
                "Please enter a vendor name.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Form.Phone))
        {
            MessageBox.Show(
                "Please enter a phone number for this vendor.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public partial class VendorProfileForm : ObservableObject
{
    [ObservableProperty] private string _vendorName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
}
