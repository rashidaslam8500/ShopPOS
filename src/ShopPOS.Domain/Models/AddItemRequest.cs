namespace ShopPOS.Domain.Models;

public class AddItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}
