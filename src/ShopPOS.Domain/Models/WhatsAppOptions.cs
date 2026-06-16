namespace ShopPOS.Domain.Models;

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "MetaCloud";
    public string ApiUrl { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ThankYouMessage { get; set; } =
        "Thank you for shopping at KitchenMart.pk (Bhai Gee Crockery Store)! Visit us again within 15 days to claim your exclusive return customer discount.";
}
