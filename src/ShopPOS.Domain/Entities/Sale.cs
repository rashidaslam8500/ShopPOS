using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal ReturnedAmount { get; set; }
    public decimal AddedAmount { get; set; }
    public decimal NetTotal { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal ChangeAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Completed;
    public int SoldByUserId { get; set; }
    public string SoldByUsername { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
    public ICollection<SaleAmendment> Amendments { get; set; } = new List<SaleAmendment>();
}
