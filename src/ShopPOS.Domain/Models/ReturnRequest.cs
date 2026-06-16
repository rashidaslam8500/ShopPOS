namespace ShopPOS.Domain.Models;

public class ReturnRequest
{
    public int SaleItemId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
