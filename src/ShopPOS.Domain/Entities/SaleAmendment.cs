using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

/// <summary>
/// Append-only log entry for invoice returns or post-sale additions.
/// </summary>
public class SaleAmendment
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public AmendmentAction Action { get; set; }
    public int? SaleItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AmountDelta { get; set; }
    public string? Reason { get; set; }
    public int ProcessedByUserId { get; set; }
    public string ProcessedByUsername { get; set; } = string.Empty;
    public DateTime AmendedAt { get; set; } = DateTime.Now;

    public Sale Sale { get; set; } = null!;
    public SaleItem? SaleItem { get; set; }
}
