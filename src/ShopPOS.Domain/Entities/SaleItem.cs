namespace ShopPOS.Domain.Entities;

/// <summary>
/// Invoice line with price frozen at time of sale for audit and return accuracy.
/// </summary>
public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPriceAtSale { get; set; }
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsAmendmentLine { get; set; }

    public Sale Sale { get; set; } = null!;
    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();

    public int RemainingQuantity => Quantity - ReturnedQuantity;
}
