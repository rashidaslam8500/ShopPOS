using System.Text.Json;

using System.Text.Json.Nodes;

using ShopPOS.Data.Security;

using ShopPOS.Domain.Enums;

using ShopPOS.Domain.Interfaces;

using ShopPOS.Domain.Models;



namespace ShopPOS.Business.Services;



public interface IIntegrationSettingsService

{

    Task<IntegrationSettings> GetAsync();

    Task SaveAsync(

        IntegrationSettings settings,

        string? newCloudApiKey,

        string? newSmsApiToken,

        string? newWhatsAppApiToken = null,

        string? newEmailPassword = null);

    Task LoadAndApplyAsync();

}



public class IntegrationSettingsService : IIntegrationSettingsService

{

    private const string KeyPrefix = "Integration.";



    private readonly ISettingsRepository _settings;

    private readonly IAuditService _audit;

    private readonly CloudSyncOptions _cloudRuntime;

    private readonly SmsOptions _smsRuntime;

    private readonly WhatsAppOptions _whatsAppRuntime;

    private readonly EmailOptions _emailRuntime;

    private readonly string _appSettingsPath;



    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };



    public IntegrationSettingsService(

        ISettingsRepository settings,

        IAuditService audit,

        CloudSyncOptions cloudRuntime,

        SmsOptions smsRuntime,

        WhatsAppOptions whatsAppRuntime,

        EmailOptions emailRuntime)

    {

        _settings = settings;

        _audit = audit;

        _cloudRuntime = cloudRuntime;

        _smsRuntime = smsRuntime;

        _whatsAppRuntime = whatsAppRuntime;

        _emailRuntime = emailRuntime;

        _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    }



    public async Task<IntegrationSettings> GetAsync()

    {

        var stored = await _settings.GetSettingsByPrefixAsync(KeyPrefix);

        if (stored.Count == 0)

            return SnapshotFromRuntime();



        var cloudKey = Get(stored, "CloudSync.ApiKey");

        var smsToken = Get(stored, "Sms.ApiKey");

        var whatsAppToken = Get(stored, "WhatsApp.ApiKey");

        var emailPassword = Get(stored, "Email.Password");



        return new IntegrationSettings

        {

            CloudSync = new CloudSyncOptions

            {

                Enabled = ParseBool(Get(stored, "CloudSync.Enabled")),

                ApiBaseUrl = Get(stored, "CloudSync.ApiBaseUrl", _cloudRuntime.ApiBaseUrl),

                ApiKey = string.Empty,

                ShopId = Get(stored, "CloudSync.ShopId", _cloudRuntime.ShopId),

                IntervalMinutes = ParseInt(Get(stored, "CloudSync.IntervalMinutes"), _cloudRuntime.IntervalMinutes)

            },

            Sms = new SmsOptions

            {

                Enabled = ParseBool(Get(stored, "Sms.Enabled")),

                Provider = Get(stored, "Sms.Provider", _smsRuntime.Provider),

                ApiUrl = Get(stored, "Sms.ApiUrl"),

                ApiUsername = SettingsSecretProtector.IsProtected(Get(stored, "Sms.ApiUsername"))

                    ? SettingsSecretProtector.Unprotect(Get(stored, "Sms.ApiUsername"))

                    : Get(stored, "Sms.ApiUsername"),

                ApiKey = string.Empty,

                SenderId = Get(stored, "Sms.SenderId", _smsRuntime.SenderId),

                ThankYouMessage = Get(stored, "Sms.ThankYouMessage", _smsRuntime.ThankYouMessage)

            },

            WhatsApp = new WhatsAppOptions

            {

                Enabled = ParseBool(Get(stored, "WhatsApp.Enabled")),

                Provider = Get(stored, "WhatsApp.Provider", _whatsAppRuntime.Provider),

                ApiUrl = Get(stored, "WhatsApp.ApiUrl"),

                PhoneNumberId = Get(stored, "WhatsApp.PhoneNumberId"),

                ApiKey = string.Empty,

                ThankYouMessage = Get(stored, "WhatsApp.ThankYouMessage", _whatsAppRuntime.ThankYouMessage)

            },

            Email = new EmailOptions

            {

                Enabled = ParseBool(Get(stored, "Email.Enabled")),

                SmtpHost = Get(stored, "Email.SmtpHost"),

                SmtpPort = ParseInt(Get(stored, "Email.SmtpPort"), _emailRuntime.SmtpPort),

                UseSsl = ParseBool(Get(stored, "Email.UseSsl", _emailRuntime.UseSsl.ToString())),

                Username = Get(stored, "Email.Username"),

                Password = string.Empty,

                FromAddress = Get(stored, "Email.FromAddress", _emailRuntime.FromAddress),

                FromName = Get(stored, "Email.FromName", _emailRuntime.FromName),

                ThankYouSubject = Get(stored, "Email.ThankYouSubject", _emailRuntime.ThankYouSubject)

            },

            HasStoredCloudApiKey = HasStoredSecret(cloudKey, _cloudRuntime.ApiKey),

            HasStoredSmsApiToken = HasStoredSecret(smsToken, _smsRuntime.ApiKey),

            HasStoredWhatsAppApiToken = HasStoredSecret(whatsAppToken, _whatsAppRuntime.ApiKey),

            HasStoredEmailPassword = HasStoredSecret(emailPassword, _emailRuntime.Password),

            LastSyncStatus = Get(stored, "CloudSync.LastSyncStatus")

        };

    }



    public async Task SaveAsync(

        IntegrationSettings settings,

        string? newCloudApiKey,

        string? newSmsApiToken,

        string? newWhatsAppApiToken = null,

        string? newEmailPassword = null)

    {

        var existing = await _settings.GetSettingsByPrefixAsync(KeyPrefix);

        var cloudKeyToStore = ResolveSecret(newCloudApiKey, Get(existing, "CloudSync.ApiKey"), settings.CloudSync.ApiKey, _cloudRuntime.ApiKey);

        var smsTokenToStore = ResolveSecret(newSmsApiToken, Get(existing, "Sms.ApiKey"), settings.Sms.ApiKey, _smsRuntime.ApiKey);

        var whatsAppTokenToStore = ResolveSecret(newWhatsAppApiToken, Get(existing, "WhatsApp.ApiKey"), settings.WhatsApp.ApiKey, _whatsAppRuntime.ApiKey);

        var emailPasswordToStore = ResolveSecret(newEmailPassword, Get(existing, "Email.Password"), settings.Email.Password, _emailRuntime.Password);



        await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.Enabled", settings.CloudSync.Enabled.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.ApiBaseUrl", settings.CloudSync.ApiBaseUrl.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.ApiKey", ProtectIfNeeded(cloudKeyToStore));

        if (!string.IsNullOrWhiteSpace(cloudKeyToStore))
        {
            var savedCloudKey = await _settings.GetSettingAsync($"{KeyPrefix}CloudSync.ApiKey");
            if (string.IsNullOrWhiteSpace(savedCloudKey))
                throw new InvalidOperationException("API sync key could not be saved to the database. Please try again.");
        }

        await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.ShopId", settings.CloudSync.ShopId.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.IntervalMinutes", settings.CloudSync.IntervalMinutes.ToString());



        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.Enabled", settings.Sms.Enabled.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.Provider", settings.Sms.Provider.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.ApiUrl", settings.Sms.ApiUrl.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.ApiUsername",

            string.IsNullOrWhiteSpace(settings.Sms.ApiUsername)

                ? string.Empty

                : ProtectIfNeeded(settings.Sms.ApiUsername.Trim()));

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.ApiKey", ProtectIfNeeded(smsTokenToStore));

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.SenderId", settings.Sms.SenderId.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Sms.ThankYouMessage", settings.Sms.ThankYouMessage.Trim());



        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.Enabled", settings.WhatsApp.Enabled.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.Provider", settings.WhatsApp.Provider.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.ApiUrl", settings.WhatsApp.ApiUrl.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.PhoneNumberId", settings.WhatsApp.PhoneNumberId.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.ApiKey", ProtectIfNeeded(whatsAppTokenToStore));

        await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.ThankYouMessage", settings.WhatsApp.ThankYouMessage.Trim());



        await _settings.SaveSettingAsync($"{KeyPrefix}Email.Enabled", settings.Email.Enabled.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.SmtpHost", settings.Email.SmtpHost.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.SmtpPort", settings.Email.SmtpPort.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.UseSsl", settings.Email.UseSsl.ToString());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.Username", settings.Email.Username.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.Password", ProtectIfNeeded(emailPasswordToStore));

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.FromAddress", settings.Email.FromAddress.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.FromName", settings.Email.FromName.Trim());

        await _settings.SaveSettingAsync($"{KeyPrefix}Email.ThankYouSubject", settings.Email.ThankYouSubject.Trim());



        ApplyToRuntime(settings, cloudKeyToStore, smsTokenToStore, whatsAppTokenToStore, emailPasswordToStore);

        await WriteAppSettingsAsync(settings, cloudKeyToStore, smsTokenToStore, whatsAppTokenToStore, emailPasswordToStore);



        await _audit.LogAsync(AuditActionType.SettingsChanged, "IntegrationSettings", "cloud+sms+whatsapp+email",

            "Cloud sync, SMS, WhatsApp, and email settings updated");

    }



    public async Task LoadAndApplyAsync()

    {

        var stored = await _settings.GetSettingsByPrefixAsync(KeyPrefix);

        if (stored.Count == 0)
        {
            await PersistMissingSecretsAsync(stored);
            return;
        }



        var cloudKey = UnprotectIfNeeded(Get(stored, "CloudSync.ApiKey"));

        var smsToken = UnprotectIfNeeded(Get(stored, "Sms.ApiKey"));

        var whatsAppToken = UnprotectIfNeeded(Get(stored, "WhatsApp.ApiKey"));

        var emailPassword = UnprotectIfNeeded(Get(stored, "Email.Password"));

        var smsUsername = Get(stored, "Sms.ApiUsername");

        if (SettingsSecretProtector.IsProtected(smsUsername))

            smsUsername = SettingsSecretProtector.Unprotect(smsUsername);



        var integration = new IntegrationSettings

        {

            CloudSync = new CloudSyncOptions

            {

                Enabled = ParseBool(Get(stored, "CloudSync.Enabled")),

                ApiBaseUrl = Get(stored, "CloudSync.ApiBaseUrl", _cloudRuntime.ApiBaseUrl),

                ShopId = Get(stored, "CloudSync.ShopId", _cloudRuntime.ShopId),

                IntervalMinutes = ParseInt(Get(stored, "CloudSync.IntervalMinutes"), _cloudRuntime.IntervalMinutes)

            },

            Sms = new SmsOptions

            {

                Enabled = ParseBool(Get(stored, "Sms.Enabled")),

                Provider = Get(stored, "Sms.Provider", _smsRuntime.Provider),

                ApiUrl = Get(stored, "Sms.ApiUrl"),

                ApiUsername = smsUsername,

                SenderId = Get(stored, "Sms.SenderId", _smsRuntime.SenderId),

                ThankYouMessage = Get(stored, "Sms.ThankYouMessage", _smsRuntime.ThankYouMessage)

            },

            WhatsApp = new WhatsAppOptions

            {

                Enabled = ParseBool(Get(stored, "WhatsApp.Enabled")),

                Provider = Get(stored, "WhatsApp.Provider", _whatsAppRuntime.Provider),

                ApiUrl = Get(stored, "WhatsApp.ApiUrl"),

                PhoneNumberId = Get(stored, "WhatsApp.PhoneNumberId"),

                ThankYouMessage = Get(stored, "WhatsApp.ThankYouMessage", _whatsAppRuntime.ThankYouMessage)

            },

            Email = new EmailOptions

            {

                Enabled = ParseBool(Get(stored, "Email.Enabled")),

                SmtpHost = Get(stored, "Email.SmtpHost"),

                SmtpPort = ParseInt(Get(stored, "Email.SmtpPort"), _emailRuntime.SmtpPort),

                UseSsl = ParseBool(Get(stored, "Email.UseSsl", _emailRuntime.UseSsl.ToString())),

                Username = Get(stored, "Email.Username"),

                FromAddress = Get(stored, "Email.FromAddress", _emailRuntime.FromAddress),

                FromName = Get(stored, "Email.FromName", _emailRuntime.FromName),

                ThankYouSubject = Get(stored, "Email.ThankYouSubject", _emailRuntime.ThankYouSubject)

            }

        };



        ApplyToRuntime(integration, cloudKey, smsToken, whatsAppToken, emailPassword);

        await PersistMissingSecretsAsync(stored);
    }



    private IntegrationSettings SnapshotFromRuntime() => new()

    {

        CloudSync = CloneCloud(_cloudRuntime),

        Sms = CloneSms(_smsRuntime),

        WhatsApp = CloneWhatsApp(_whatsAppRuntime),

        Email = CloneEmail(_emailRuntime),

        HasStoredCloudApiKey = !string.IsNullOrWhiteSpace(_cloudRuntime.ApiKey),

        HasStoredSmsApiToken = !string.IsNullOrWhiteSpace(_smsRuntime.ApiKey),

        HasStoredWhatsAppApiToken = !string.IsNullOrWhiteSpace(_whatsAppRuntime.ApiKey),

        HasStoredEmailPassword = !string.IsNullOrWhiteSpace(_emailRuntime.Password)

    };



    private void ApplyToRuntime(

        IntegrationSettings settings,

        string cloudApiKey,

        string smsApiToken,

        string whatsAppApiToken,

        string emailPassword)

    {

        _cloudRuntime.Enabled = settings.CloudSync.Enabled;

        _cloudRuntime.ApiBaseUrl = string.IsNullOrWhiteSpace(settings.CloudSync.ApiBaseUrl)
            ? _cloudRuntime.ApiBaseUrl
            : settings.CloudSync.ApiBaseUrl;

        _cloudRuntime.ApiKey = CoalesceSecret(cloudApiKey, _cloudRuntime.ApiKey);

        _cloudRuntime.ShopId = settings.CloudSync.ShopId;

        _cloudRuntime.IntervalMinutes = Math.Max(1, settings.CloudSync.IntervalMinutes);



        _smsRuntime.Enabled = settings.Sms.Enabled;

        _smsRuntime.Provider = settings.Sms.Provider;

        _smsRuntime.ApiUrl = settings.Sms.ApiUrl;

        _smsRuntime.ApiUsername = settings.Sms.ApiUsername;

        _smsRuntime.ApiKey = CoalesceSecret(smsApiToken, _smsRuntime.ApiKey);

        _smsRuntime.SenderId = settings.Sms.SenderId;

        _smsRuntime.ThankYouMessage = settings.Sms.ThankYouMessage;



        _whatsAppRuntime.Enabled = settings.WhatsApp.Enabled;

        _whatsAppRuntime.Provider = settings.WhatsApp.Provider;

        _whatsAppRuntime.ApiUrl = settings.WhatsApp.ApiUrl;

        _whatsAppRuntime.PhoneNumberId = settings.WhatsApp.PhoneNumberId;

        _whatsAppRuntime.ApiKey = CoalesceSecret(whatsAppApiToken, _whatsAppRuntime.ApiKey);

        _whatsAppRuntime.ThankYouMessage = settings.WhatsApp.ThankYouMessage;



        _emailRuntime.Enabled = settings.Email.Enabled;

        _emailRuntime.SmtpHost = settings.Email.SmtpHost;

        _emailRuntime.SmtpPort = settings.Email.SmtpPort;

        _emailRuntime.UseSsl = settings.Email.UseSsl;

        _emailRuntime.Username = settings.Email.Username;

        _emailRuntime.Password = CoalesceSecret(emailPassword, _emailRuntime.Password);

        _emailRuntime.FromAddress = settings.Email.FromAddress;

        _emailRuntime.FromName = settings.Email.FromName;

        _emailRuntime.ThankYouSubject = settings.Email.ThankYouSubject;

    }



    private async Task WriteAppSettingsAsync(

        IntegrationSettings settings,

        string cloudApiKey,

        string smsApiToken,

        string whatsAppApiToken,

        string emailPassword)

    {

        if (!File.Exists(_appSettingsPath))

            return;



        try

        {

            var jsonText = await File.ReadAllTextAsync(_appSettingsPath);

            var root = JsonNode.Parse(jsonText)?.AsObject() ?? new JsonObject();



            root["CloudSync"] = new JsonObject

            {

                ["Enabled"] = settings.CloudSync.Enabled,

                ["ApiBaseUrl"] = settings.CloudSync.ApiBaseUrl,

                ["ApiKey"] = ProtectIfNeeded(cloudApiKey),

                ["ShopId"] = settings.CloudSync.ShopId,

                ["IntervalMinutes"] = settings.CloudSync.IntervalMinutes

            };



            root["Sms"] = new JsonObject

            {

                ["Enabled"] = settings.Sms.Enabled,

                ["Provider"] = settings.Sms.Provider,

                ["ApiUrl"] = settings.Sms.ApiUrl,

                ["ApiUsername"] = string.IsNullOrWhiteSpace(settings.Sms.ApiUsername)

                    ? string.Empty

                    : ProtectIfNeeded(settings.Sms.ApiUsername),

                ["ApiKey"] = ProtectIfNeeded(smsApiToken),

                ["SenderId"] = settings.Sms.SenderId,

                ["ThankYouMessage"] = settings.Sms.ThankYouMessage

            };



            root["WhatsApp"] = new JsonObject

            {

                ["Enabled"] = settings.WhatsApp.Enabled,

                ["Provider"] = settings.WhatsApp.Provider,

                ["ApiUrl"] = settings.WhatsApp.ApiUrl,

                ["PhoneNumberId"] = settings.WhatsApp.PhoneNumberId,

                ["ApiKey"] = ProtectIfNeeded(whatsAppApiToken),

                ["ThankYouMessage"] = settings.WhatsApp.ThankYouMessage

            };



            root["Email"] = new JsonObject

            {

                ["Enabled"] = settings.Email.Enabled,

                ["SmtpHost"] = settings.Email.SmtpHost,

                ["SmtpPort"] = settings.Email.SmtpPort,

                ["UseSsl"] = settings.Email.UseSsl,

                ["Username"] = settings.Email.Username,

                ["Password"] = ProtectIfNeeded(emailPassword),

                ["FromAddress"] = settings.Email.FromAddress,

                ["FromName"] = settings.Email.FromName,

                ["ThankYouSubject"] = settings.Email.ThankYouSubject

            };



            await File.WriteAllTextAsync(_appSettingsPath, root.ToJsonString(JsonOptions));

        }

        catch

        {

            // Non-fatal: database remains source of truth at runtime.

        }

    }



    private static bool HasStoredSecret(string storedValue, string runtimeValue) =>
        !string.IsNullOrWhiteSpace(storedValue) || !string.IsNullOrWhiteSpace(runtimeValue);

    private async Task PersistMissingSecretsAsync(IReadOnlyDictionary<string, string> stored)
    {
        if (string.IsNullOrWhiteSpace(Get(stored, "CloudSync.ApiKey")) && !string.IsNullOrWhiteSpace(_cloudRuntime.ApiKey))
            await _settings.SaveSettingAsync($"{KeyPrefix}CloudSync.ApiKey", ProtectIfNeeded(_cloudRuntime.ApiKey));

        if (string.IsNullOrWhiteSpace(Get(stored, "Sms.ApiKey")) && !string.IsNullOrWhiteSpace(_smsRuntime.ApiKey))
            await _settings.SaveSettingAsync($"{KeyPrefix}Sms.ApiKey", ProtectIfNeeded(_smsRuntime.ApiKey));

        if (string.IsNullOrWhiteSpace(Get(stored, "WhatsApp.ApiKey")) && !string.IsNullOrWhiteSpace(_whatsAppRuntime.ApiKey))
            await _settings.SaveSettingAsync($"{KeyPrefix}WhatsApp.ApiKey", ProtectIfNeeded(_whatsAppRuntime.ApiKey));

        if (string.IsNullOrWhiteSpace(Get(stored, "Email.Password")) && !string.IsNullOrWhiteSpace(_emailRuntime.Password))
            await _settings.SaveSettingAsync($"{KeyPrefix}Email.Password", ProtectIfNeeded(_emailRuntime.Password));
    }

    private static string CoalesceSecret(string loaded, string current) =>
        string.IsNullOrWhiteSpace(loaded) ? current : loaded;

    private static string ResolveSecret(string? newValue, string existingStored, string modelValue, string? runtimeFallback = null)

    {

        if (!string.IsNullOrWhiteSpace(newValue))

            return newValue.Trim();



        if (!string.IsNullOrWhiteSpace(existingStored))

            return UnprotectIfNeeded(existingStored);



        if (!string.IsNullOrWhiteSpace(modelValue))

            return modelValue.Trim();



        return runtimeFallback?.Trim() ?? string.Empty;

    }



    private static string ProtectIfNeeded(string value) =>

        string.IsNullOrWhiteSpace(value) ? string.Empty : SettingsSecretProtector.Protect(value);



    private static string UnprotectIfNeeded(string value) =>

        string.IsNullOrWhiteSpace(value)

            ? string.Empty

            : SettingsSecretProtector.IsProtected(value)

                ? SettingsSecretProtector.Unprotect(value)

                : value;



    private static string Get(IReadOnlyDictionary<string, string> stored, string suffix, string fallback = "") =>

        stored.TryGetValue($"{KeyPrefix}{suffix}", out var value) ? value : fallback;



    private static bool ParseBool(string value) =>

        bool.TryParse(value, out var result) && result;



    private static int ParseInt(string value, int fallback) =>

        int.TryParse(value, out var result) ? result : fallback;



    private static CloudSyncOptions CloneCloud(CloudSyncOptions source) => new()

    {

        Enabled = source.Enabled,

        ApiBaseUrl = source.ApiBaseUrl,

        ApiKey = source.ApiKey,

        ShopId = source.ShopId,

        IntervalMinutes = source.IntervalMinutes

    };



    private static SmsOptions CloneSms(SmsOptions source) => new()

    {

        Enabled = source.Enabled,

        Provider = source.Provider,

        ApiUrl = source.ApiUrl,

        ApiUsername = source.ApiUsername,

        ApiKey = source.ApiKey,

        SenderId = source.SenderId,

        ThankYouMessage = source.ThankYouMessage

    };



    private static WhatsAppOptions CloneWhatsApp(WhatsAppOptions source) => new()

    {

        Enabled = source.Enabled,

        Provider = source.Provider,

        ApiUrl = source.ApiUrl,

        PhoneNumberId = source.PhoneNumberId,

        ApiKey = source.ApiKey,

        ThankYouMessage = source.ThankYouMessage

    };



    private static EmailOptions CloneEmail(EmailOptions source) => new()

    {

        Enabled = source.Enabled,

        SmtpHost = source.SmtpHost,

        SmtpPort = source.SmtpPort,

        UseSsl = source.UseSsl,

        Username = source.Username,

        Password = source.Password,

        FromAddress = source.FromAddress,

        FromName = source.FromName,

        ThankYouSubject = source.ThankYouSubject

    };

}


