namespace ShopPOS.Domain.Models;

public class PosHardwareOptions
{
    public const string SectionName = "Hardware";

    public string LogoPath { get; set; } = "Assets\\shop-logo.png";

    public string? ThermalPrinterName { get; set; }
    public bool OpenCashDrawerOnPrint { get; set; } = true;
}
