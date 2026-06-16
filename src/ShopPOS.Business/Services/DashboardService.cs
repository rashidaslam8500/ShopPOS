using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductRepository _products;
    private readonly ISaleRepository _sales;
    private readonly IExpenseAndCashRepository _expenses;
    private readonly IOwnerPersonalExpenseRepository _ownerExpenses;

    public DashboardService(
        IProductRepository products,
        ISaleRepository sales,
        IExpenseAndCashRepository expenses,
        IOwnerPersonalExpenseRepository ownerExpenses)
    {
        _products = products;
        _sales = sales;
        _expenses = expenses;
        _ownerExpenses = ownerExpenses;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var products = await _products.GetAllAsync();
        var sales = await _sales.GetAllAsync();
        var today = DateTime.Today;
        var todaySales = sales.Where(s => s.SaleDate.Date == today).ToList();

        var todayRevenue = todaySales.Sum(ReceiptFormatter.GetNetTotal);
        var allTimeRevenue = sales.Sum(ReceiptFormatter.GetNetTotal);
        var shopTodayExpenses = await _expenses.GetTotalExpensesForDateAsync(today);
        var shopAllTimeExpenses = await _expenses.GetTotalExpensesSinceAsync(DateTime.MinValue);
        var ownerTodayExpenses = await _ownerExpenses.GetTotalSinceAsync(today);
        var ownerAllTimeExpenses = await _ownerExpenses.GetTotalSinceAsync(DateTime.MinValue);
        var todayExpenses = shopTodayExpenses + ownerTodayExpenses;
        var allTimeExpenses = shopAllTimeExpenses + ownerAllTimeExpenses;

        var topProducts = sales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductStat { ProductName = g.Key, QuantitySold = g.Sum(i => i.Quantity) })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        var recentSales = sales
            .OrderByDescending(s => s.SaleDate)
            .Take(5)
            .Select(s => new RecentSaleStat
            {
                ReceiptNo = s.ReceiptNo,
                Total = s.Total,
                SaleDate = s.SaleDate
            })
            .ToList();

        return new DashboardStats
        {
            TodayRevenue = todayRevenue,
            TodayTransactionCount = todaySales.Count,
            TotalProducts = products.Count,
            LowStockCount = products.Count(p => p.Stock < 10),
            AllTimeRevenue = allTimeRevenue,
            TotalSalesCount = sales.Count,
            TodayExpenses = todayExpenses,
            TodayNetProfit = todayRevenue - todayExpenses,
            AllTimeExpenses = allTimeExpenses,
            AllTimeNetProfit = allTimeRevenue - allTimeExpenses,
            TopProducts = topProducts,
            RecentSales = recentSales
        };
    }
}
