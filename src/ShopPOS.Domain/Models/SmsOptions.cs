namespace ShopPOS.Domain.Models;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "GenericHttp";
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiUsername { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = "KitchenMart";
    public string ThankYouMessage { get; set; } =
        "Thank you for shopping at KitchenMart.pk (Bhai Gee Crockery Store)! Visit us again within 15 days to claim your exclusive return customer discount.";
}
