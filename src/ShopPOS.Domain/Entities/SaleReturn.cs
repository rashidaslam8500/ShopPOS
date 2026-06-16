namespace ShopPOS.Domain.Entities;

/// <summary>
/// Return line linked to the original invoice item.
/// Refund uses UnitPriceAtOriginalSale — never the current inventory price.
/// </summary>
public class SaleReturn
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int SaleItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceAtOriginalSale { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Reason { get; set; }
    public int ProcessedByUserId { get; set; }
    public string ProcessedByUsername { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.Now;

    public Sale Sale { get; set; } = null!;
    public SaleItem SaleItem { get; set; } = null!;
}
