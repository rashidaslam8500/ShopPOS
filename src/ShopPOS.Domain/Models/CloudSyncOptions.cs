namespace ShopPOS.Domain.Models;

public class CloudSyncOptions
{
    public const string SectionName = "CloudSync";

    public bool Enabled { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.kitchenmart.pk/pos";
    public string ApiKey { get; set; } = string.Empty;
    public string ShopId { get; set; } = "kitchenmart-sargodha";
    public int IntervalMinutes { get; set; } = 5;
}
