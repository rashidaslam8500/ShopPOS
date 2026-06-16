namespace ShopPOS.Domain.Models;

public class ShopConfig
{
    public string ShopName { get; set; } = "Bhai Gee Crockery Store";
    public string Address { get; set; } = "Karkhana Bazar, Sargodha";
    public string Phone { get; set; } = "03006027106";
    public string CurrencySymbol { get; set; } = "Rs.";
    public decimal TaxRate { get; set; }
    public string ReceiptFooter { get; set; } = "Thank you for shopping at Bhai Gee Crockery Store!";
}
