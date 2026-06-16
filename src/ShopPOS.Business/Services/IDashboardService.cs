using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}
