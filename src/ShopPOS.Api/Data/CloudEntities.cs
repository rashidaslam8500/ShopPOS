namespace ShopPOS.Api.Data;

public class CloudSale
{
    public int Id { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public int LocalSaleId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal NetTotal { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ItemsJson { get; set; } = "[]";
    public DateTime SyncedAtUtc { get; set; }
}

public class CloudProduct
{
    public int Id { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public int LocalProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

public class CloudCustomer
{
    public int Id { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public int LocalCustomerId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
    public int VisitCount { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

public class CloudSyncLog
{
    public int Id { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public DateTime SyncedAtUtc { get; set; }
    public int SalesCount { get; set; }
    public int ProductsCount { get; set; }
    public int CustomersCount { get; set; }
}
