using ShopPOS.Business.Services;
using System.Windows;
namespace ShopPOS.WPF.Windows;

public partial class FingerprintEnrollDialog : Window
{
    private readonly IFingerprintScannerService _fingerprint;

    public string? CapturedTemplate { get; private set; }

    public FingerprintEnrollDialog(IFingerprintScannerService fingerprint)
    {
        _fingerprint = fingerprint;
        InitializeComponent();
        Loaded += async (_, _) => await RunCaptureAsync();
    }

    private async Task RunCaptureAsync()
    {
        StatusText.Text = "Please place thumb on the scanner...";
        Spinner.Visibility = Visibility.Visible;

        try
        {
            var result = await _fingerprint.CaptureEnrollmentAsync();
            if (!result.Success || string.IsNullOrWhiteSpace(result.TemplateBase64))
            {
                StatusText.Text = result.Message;
                return;
            }

            CapturedTemplate = result.TemplateBase64;
            StatusText.Text = "Thumbprint captured successfully!";
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Capture failed: {ex.Message}";
        }
        finally
        {
            Spinner.Visibility = Visibility.Collapsed;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
