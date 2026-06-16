namespace ShopPOS.Domain.Models;

public class IntegrationSettings
{
    public CloudSyncOptions CloudSync { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();
    public WhatsAppOptions WhatsApp { get; set; } = new();
    public EmailOptions Email { get; set; } = new();
    public bool HasStoredCloudApiKey { get; set; }
    public bool HasStoredSmsApiToken { get; set; }
    public bool HasStoredWhatsAppApiToken { get; set; }
    public bool HasStoredEmailPassword { get; set; }
    public string? LastSyncStatus { get; set; }
}
