using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;
using ShopPOS.Domain.Security;

namespace ShopPOS.WPF.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IIntegrationSettingsService _integration;
    private readonly IAuthService _auth;
    private readonly IAppShell _shell;
    private readonly ICloudSyncService _cloudSync;

    [ObservableProperty] private string _shopName = "Bhai Gee Crockery Store";
    [ObservableProperty] private string _address = "Karkhana Bazar, Sargodha";
    [ObservableProperty] private string _phone = "03006027106";
    [ObservableProperty] private string _currencySymbol = "Rs.";
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string _receiptFooter = string.Empty;

    [ObservableProperty] private bool _cloudSyncEnabled;
    [ObservableProperty] private string _cloudServerUrl = string.Empty;
    [ObservableProperty] private string _cloudShopId = "kitchenmart-sargodha";
    [ObservableProperty] private int _cloudSyncIntervalMinutes = 5;
    [ObservableProperty] private string _cloudApiKeyHint = string.Empty;
    [ObservableProperty] private string _cloudApiKeyInput = string.Empty;
    [ObservableProperty] private bool _isCloudApiKeyStored;
    [ObservableProperty] private string _cloudSyncStatus = "Not synced yet.";

    [ObservableProperty] private bool _smsEnabled;
    [ObservableProperty] private string _smsProvider = "GenericHttp";
    [ObservableProperty] private string _smsGatewayUrl = string.Empty;
    [ObservableProperty] private string _smsApiUsername = string.Empty;
    [ObservableProperty] private string _smsSenderId = "KitchenMart";
    [ObservableProperty] private string _smsApiTokenHint = string.Empty;
    [ObservableProperty] private string _smsThankYouMessage =
        "Thank you for shopping at KitchenMart.pk (Bhai Gee Crockery Store)! Visit us again within 15 days to claim your exclusive return customer discount.";

    [ObservableProperty] private bool _whatsAppEnabled;
    [ObservableProperty] private string _whatsAppProvider = "MetaCloud";
    [ObservableProperty] private string _whatsAppApiUrl = string.Empty;
    [ObservableProperty] private string _whatsAppPhoneNumberId = string.Empty;
    [ObservableProperty] private string _whatsAppApiTokenHint = string.Empty;
    [ObservableProperty] private string _whatsAppThankYouMessage =
        "Thank you for shopping at KitchenMart.pk (Bhai Gee Crockery Store)! Visit us again within 15 days to claim your exclusive return customer discount.";

    [ObservableProperty] private bool _emailEnabled;
    [ObservableProperty] private string _emailSmtpHost = string.Empty;
    [ObservableProperty] private int _emailSmtpPort = 587;
    [ObservableProperty] private bool _emailUseSsl = true;
    [ObservableProperty] private string _emailUsername = string.Empty;
    [ObservableProperty] private string _emailPasswordHint = string.Empty;
    [ObservableProperty] private string _emailFromAddress = string.Empty;
    [ObservableProperty] private string _emailFromName = "KitchenMart";
    [ObservableProperty] private string _emailThankYouSubject = "Thank you for shopping at KitchenMart.pk";

    public IReadOnlyList<string> AvailableSecurityQuestions => SecurityQuestionCatalog.StandardQuestions;
    [ObservableProperty] private string? _securityQuestion1;
    [ObservableProperty] private string? _securityQuestion2;

    public SettingsViewModel(
        ISettingsService settings,
        IIntegrationSettingsService integration,
        IAuthService auth,
        IAppShell shell,
        ICloudSyncService cloudSync)
    {
        _settings = settings;
        _integration = integration;
        _auth = auth;
        _shell = shell;
        _cloudSync = cloudSync;
        _ = LoadAsync();
    }

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        var config = await _settings.GetConfigAsync();
        ShopName = config.ShopName;
        Address = config.Address;
        Phone = config.Phone;
        CurrencySymbol = config.CurrencySymbol;
        TaxRate = config.TaxRate;
        ReceiptFooter = config.ReceiptFooter;

        var integration = await _integration.GetAsync();

        CloudSyncEnabled = integration.CloudSync.Enabled;
        CloudServerUrl = integration.CloudSync.ApiBaseUrl;
        CloudShopId = integration.CloudSync.ShopId;
        CloudSyncIntervalMinutes = integration.CloudSync.IntervalMinutes;
        CloudApiKeyHint = integration.HasStoredCloudApiKey
            ? "API key is saved. The field is left empty for security — type a new key only to replace it."
            : "Enter your cloud API sync key (example: kitchenmart-dev-key).";
        IsCloudApiKeyStored = integration.HasStoredCloudApiKey;
        CloudApiKeyInput = string.Empty;
        CloudSyncStatus = integration.LastSyncStatus ?? _cloudSync.LastSyncStatus ?? "Not synced yet.";

        SmsEnabled = integration.Sms.Enabled;
        SmsProvider = integration.Sms.Provider;
        SmsGatewayUrl = integration.Sms.ApiUrl;
        SmsApiUsername = integration.Sms.ApiUsername;
        SmsSenderId = integration.Sms.SenderId;
        SmsThankYouMessage = integration.Sms.ThankYouMessage;
        SmsApiTokenHint = integration.HasStoredSmsApiToken
            ? "A token is saved. Enter a new token only to replace it."
            : "Enter your SMS gateway API token.";

        WhatsAppEnabled = integration.WhatsApp.Enabled;
        WhatsAppProvider = integration.WhatsApp.Provider;
        WhatsAppApiUrl = integration.WhatsApp.ApiUrl;
        WhatsAppPhoneNumberId = integration.WhatsApp.PhoneNumberId;
        WhatsAppThankYouMessage = integration.WhatsApp.ThankYouMessage;
        WhatsAppApiTokenHint = integration.HasStoredWhatsAppApiToken
            ? "A token is saved. Enter a new token only to replace it."
            : "Enter your WhatsApp Cloud API access token.";

        EmailEnabled = integration.Email.Enabled;
        EmailSmtpHost = integration.Email.SmtpHost;
        EmailSmtpPort = integration.Email.SmtpPort;
        EmailUseSsl = integration.Email.UseSsl;
        EmailUsername = integration.Email.Username;
        EmailFromAddress = integration.Email.FromAddress;
        EmailFromName = integration.Email.FromName;
        EmailThankYouSubject = integration.Email.ThankYouSubject;
        EmailPasswordHint = integration.HasStoredEmailPassword
            ? "A password is saved. Enter a new password only to replace it."
            : "Enter your SMTP password or app password.";
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _cloudSync.SyncAsync();
            CloudSyncStatus = _cloudSync.LastSyncStatus ?? "Sync completed.";
        }, "Cloud sync finished.");
    }

    public async Task<bool> SaveAsync(
        string? cloudApiKey,
        string? smsApiToken,
        string? whatsAppApiToken,
        string? emailPassword)
    {
        var saved = await RunSafeAsync(async () =>
        {
            await _settings.SaveConfigAsync(new ShopConfig
            {
                ShopName = ShopName,
                Address = Address,
                Phone = Phone,
                CurrencySymbol = CurrencySymbol,
                TaxRate = TaxRate,
                ReceiptFooter = ReceiptFooter
            });
            _shell.RefreshShopName(ShopName);

            await _integration.SaveAsync(new IntegrationSettings
            {
                CloudSync = new CloudSyncOptions
                {
                    Enabled = CloudSyncEnabled,
                    ApiBaseUrl = CloudServerUrl,
                    ShopId = CloudShopId,
                    IntervalMinutes = CloudSyncIntervalMinutes
                },
                Sms = new SmsOptions
                {
                    Enabled = SmsEnabled,
                    Provider = SmsProvider,
                    ApiUrl = SmsGatewayUrl,
                    ApiUsername = SmsApiUsername,
                    SenderId = SmsSenderId,
                    ThankYouMessage = SmsThankYouMessage
                },
                WhatsApp = new WhatsAppOptions
                {
                    Enabled = WhatsAppEnabled,
                    Provider = WhatsAppProvider,
                    ApiUrl = WhatsAppApiUrl,
                    PhoneNumberId = WhatsAppPhoneNumberId,
                    ThankYouMessage = WhatsAppThankYouMessage
                },
                Email = new EmailOptions
                {
                    Enabled = EmailEnabled,
                    SmtpHost = EmailSmtpHost,
                    SmtpPort = EmailSmtpPort,
                    UseSsl = EmailUseSsl,
                    Username = EmailUsername,
                    FromAddress = EmailFromAddress,
                    FromName = EmailFromName,
                    ThankYouSubject = EmailThankYouSubject
                }
            }, cloudApiKey, smsApiToken, whatsAppApiToken, emailPassword);

            await LoadAsync();
        }, "Shop and integration settings saved. Changes are active immediately.");

        if (saved)
        {
            System.Windows.MessageBox.Show(
                "Shop and integration settings saved successfully.\n\nChanges are active immediately.",
                "Settings Saved",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        return saved;
    }

    public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
    {
        return await RunSafeAsync(async () =>
        {
            await _auth.ChangePasswordAsync(oldPassword, newPassword, confirmPassword);
        }, "Password updated successfully.");
    }

    public async Task SaveSecurityQuestionsAsync(string answer1, string answer2)
    {
        await RunSafeAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(SecurityQuestion1) || string.IsNullOrWhiteSpace(SecurityQuestion2))
                throw new InvalidOperationException("Select two security questions.");

            await _auth.SetSecurityQuestionsAsync(
                SecurityQuestion1,
                answer1,
                SecurityQuestion2,
                answer2);
        }, "Security questions saved.");
    }
}
