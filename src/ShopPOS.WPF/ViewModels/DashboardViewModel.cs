using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboard;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _todayRevenue = "Rs. 0";
    [ObservableProperty] private string _todayCount = "0 transactions";
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _lowStock;
    [ObservableProperty] private string _allTimeRevenue = "Rs. 0";
    [ObservableProperty] private string _totalSales = "0 total sales";
    [ObservableProperty] private string _todayExpenses = "Rs. 0";
    [ObservableProperty] private string _todayNetProfit = "Rs. 0";
    [ObservableProperty] private string _allTimeNetProfit = "Rs. 0";

    public ObservableCollection<TopProductStat> TopProducts { get; } = new();
    public ObservableCollection<RecentSaleStat> RecentSales { get; } = new();

    public DashboardViewModel(IDashboardService dashboard, ISettingsService settings)
    {
        _dashboard = dashboard;
        _settings = settings;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var stats = await _dashboard.GetStatsAsync();
            TodayRevenue = _settings.FormatMoney(stats.TodayRevenue);
            TodayCount = $"{stats.TodayTransactionCount} transaction(s)";
            TotalProducts = stats.TotalProducts;
            LowStock = stats.LowStockCount;
            AllTimeRevenue = _settings.FormatMoney(stats.AllTimeRevenue);
            TotalSales = $"{stats.TotalSalesCount} total sales";
            TodayExpenses = _settings.FormatMoney(stats.TodayExpenses);
            TodayNetProfit = _settings.FormatMoney(stats.TodayNetProfit);
            AllTimeNetProfit = _settings.FormatMoney(stats.AllTimeNetProfit);

            TopProducts.Clear();
            foreach (var p in stats.TopProducts) TopProducts.Add(p);

            RecentSales.Clear();
            foreach (var s in stats.RecentSales) RecentSales.Add(s);
        });
    }
}
