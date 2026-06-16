using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class VendorKhataEntry
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public DateTime Date { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalBill { get; set; }
    public decimal CashPaid { get; set; }
    public VendorKhataPaymentMode PaymentMode { get; set; } = VendorKhataPaymentMode.Cash;
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Vendor Vendor { get; set; } = null!;
    public decimal RemainingBalance => TotalBill - CashPaid;
}
