using ShopPOS.Domain.Models;

namespace ShopPOS.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<ShopConfig> GetConfigAsync();
    Task SaveConfigAsync(ShopConfig config);
    Task<string?> GetSettingAsync(string key);
    Task SaveSettingAsync(string key, string value);
    Task<IReadOnlyDictionary<string, string>> GetSettingsByPrefixAsync(string prefix);
}
