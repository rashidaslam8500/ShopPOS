namespace ShopPOS.Domain.Models;

public class DashboardStats
{
    public decimal TodayRevenue { get; set; }
    public int TodayTransactionCount { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public decimal AllTimeRevenue { get; set; }
    public int TotalSalesCount { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal TodayNetProfit { get; set; }
    public decimal AllTimeExpenses { get; set; }
    public decimal AllTimeNetProfit { get; set; }
    public IReadOnlyList<TopProductStat> TopProducts { get; set; } = [];
    public IReadOnlyList<RecentSaleStat> RecentSales { get; set; } = [];
}

public class TopProductStat
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
}

public class RecentSaleStat
{
    public string ReceiptNo { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime SaleDate { get; set; }
}
