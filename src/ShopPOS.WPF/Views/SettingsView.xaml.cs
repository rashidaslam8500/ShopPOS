using System.Windows;
using System.Windows.Controls;
using ShopPOS.WPF.ViewModels;

namespace ShopPOS.WPF.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private SettingsViewModel? ResolveViewModel()
    {
        if (DataContext is SettingsViewModel direct)
            return direct;

        if (DataContext is TabPageViewModel tab && tab.Content is SettingsViewModel nested)
            return nested;

        return null;
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        if (ResolveViewModel() is not { } vm)
        {
            ShowViewModelMissingError();
            return;
        }

        var saved = await vm.ChangePasswordAsync(
            OldPasswordBox.Password,
            NewPasswordBox.Password,
            ConfirmPasswordBox.Password);

        if (saved)
        {
            OldPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }

    private async void SaveShopSettings_Click(object sender, RoutedEventArgs e)
    {
        if (ResolveViewModel() is not { } vm)
        {
            ShowViewModelMissingError();
            return;
        }

        if (vm.IsBusy)
            return;

        var cloudKey = string.IsNullOrWhiteSpace(vm.CloudApiKeyInput) ? null : vm.CloudApiKeyInput.Trim();
        var smsToken = string.IsNullOrWhiteSpace(SmsApiTokenBox.Password) ? null : SmsApiTokenBox.Password;
        var whatsAppToken = string.IsNullOrWhiteSpace(WhatsAppApiTokenBox.Password) ? null : WhatsAppApiTokenBox.Password;
        var emailPassword = string.IsNullOrWhiteSpace(EmailPasswordBox.Password) ? null : EmailPasswordBox.Password;

        var saved = await vm.SaveAsync(cloudKey, smsToken, whatsAppToken, emailPassword);

        if (saved)
        {
            vm.CloudApiKeyInput = string.Empty;
            SmsApiTokenBox.Clear();
            WhatsAppApiTokenBox.Clear();
            EmailPasswordBox.Clear();
        }
    }

    private async void SaveSecurityQuestions_Click(object sender, RoutedEventArgs e)
    {
        if (ResolveViewModel() is not { } vm)
        {
            ShowViewModelMissingError();
            return;
        }

        await vm.SaveSecurityQuestionsAsync(SecurityAnswer1Box.Text, SecurityAnswer2Box.Text);

        SecurityAnswer1Box.Clear();
        SecurityAnswer2Box.Clear();
    }

    private static void ShowViewModelMissingError()
    {
        MessageBox.Show(
            "Settings could not connect to the save handler.\n\nClose the Settings tab and open it again from the sidebar.",
            "Settings Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
