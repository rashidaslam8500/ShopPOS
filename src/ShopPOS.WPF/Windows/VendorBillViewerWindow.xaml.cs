using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ShopPOS.WPF.Services;

namespace ShopPOS.WPF.Windows;

public partial class VendorBillViewerWindow : Window
{
    private readonly IVendorBillStorageService _storage;
    private string? _attachmentPath;

    public VendorBillViewerWindow(IVendorBillStorageService storage)
    {
        InitializeComponent();
        _storage = storage;
    }

    public void LoadAttachment(string attachmentPath)
    {
        _attachmentPath = attachmentPath;
        BillImage.Visibility = Visibility.Collapsed;
        PdfPanel.Visibility = Visibility.Collapsed;

        if (!_storage.FileExists(attachmentPath))
        {
            MessageBox.Show(
                "The bill attachment file could not be found on this computer.",
                "Attachment Missing",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (_storage.IsImageFile(attachmentPath))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(attachmentPath, UriKind.Absolute);
            bitmap.EndInit();
            BillImage.Source = bitmap;
            BillImage.Visibility = Visibility.Visible;
            return;
        }

        if (_storage.IsPdfFile(attachmentPath))
        {
            PdfPanel.Visibility = Visibility.Visible;
            return;
        }

        MessageBox.Show(
            "This attachment type cannot be previewed inside the application.",
            "Preview Unavailable",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_attachmentPath) || !File.Exists(_attachmentPath))
            return;

        Process.Start(new ProcessStartInfo(_attachmentPath) { UseShellExecute = true });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
