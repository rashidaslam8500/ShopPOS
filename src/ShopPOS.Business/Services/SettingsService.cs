using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface ISettingsService
{
    Task<ShopConfig> GetConfigAsync();
    Task SaveConfigAsync(ShopConfig config);
    string FormatMoney(decimal amount, ShopConfig? config = null);
}

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settings;
    private readonly IAuditService _audit;
    private ShopConfig? _cached;

    public SettingsService(ISettingsRepository settings, IAuditService audit)
    {
        _settings = settings;
        _audit = audit;
    }

    public async Task<ShopConfig> GetConfigAsync()
    {
        _cached ??= await _settings.GetConfigAsync();
        return _cached;
    }

    public async Task SaveConfigAsync(ShopConfig config)
    {
        var old = _cached ?? await _settings.GetConfigAsync();
        await _settings.SaveConfigAsync(config);
        _cached = config;
        await _audit.LogAsync(AuditActionType.SettingsChanged, "ShopSettings", "config",
            "Shop settings updated", oldValues: old.ShopName, newValues: config.ShopName);
    }

    public string FormatMoney(decimal amount, ShopConfig? config = null)
    {
        var symbol = config?.CurrencySymbol ?? _cached?.CurrencySymbol ?? "Rs.";
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        return $"{symbol} {rounded:N0}";
    }
}
