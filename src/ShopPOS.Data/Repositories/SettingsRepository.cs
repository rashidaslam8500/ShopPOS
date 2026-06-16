using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Data.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly PosDbContext _db;

    private static readonly Dictionary<string, string> Defaults = new()
    {
        ["ShopName"] = "Bhai Gee Crockery Store",
        ["Address"] = "Karkhana Bazar, Sargodha",
        ["Phone"] = "03006027106",
        ["CurrencySymbol"] = "Rs.",
        ["TaxRate"] = "0",
        ["ReceiptFooter"] = "Thank you for shopping at Bhai Gee Crockery Store!"
    };

    public SettingsRepository(PosDbContext db) => _db = db;

    public async Task<ShopConfig> GetConfigAsync()
    {
        var settings = await _db.ShopSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        return new ShopConfig
        {
            ShopName = Get(settings, "ShopName"),
            Address = Get(settings, "Address"),
            Phone = Get(settings, "Phone"),
            CurrencySymbol = Get(settings, "CurrencySymbol"),
            TaxRate = decimal.TryParse(Get(settings, "TaxRate"), out var tax) ? tax : 0,
            ReceiptFooter = Get(settings, "ReceiptFooter")
        };
    }

    public async Task SaveConfigAsync(ShopConfig config)
    {
        await UpsertAsync("ShopName", config.ShopName);
        await UpsertAsync("Address", config.Address);
        await UpsertAsync("Phone", config.Phone);
        await UpsertAsync("CurrencySymbol", config.CurrencySymbol);
        await UpsertAsync("TaxRate", config.TaxRate.ToString("0.##"));
        await UpsertAsync("ReceiptFooter", config.ReceiptFooter);
        await _db.SaveChangesAsync();
    }

    public async Task<string?> GetSettingAsync(string key) =>
        await _db.ShopSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

    public async Task SaveSettingAsync(string key, string value)
    {
        await UpsertAsync(key, value);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSettingsByPrefixAsync(string prefix) =>
        await _db.ShopSettings.AsNoTracking()
            .Where(s => s.Key.StartsWith(prefix))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

    private static string Get(Dictionary<string, string> settings, string key) =>
        settings.TryGetValue(key, out var value) ? value : Defaults[key];

    private async Task UpsertAsync(string key, string value)
    {
        var setting = await _db.ShopSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
            _db.ShopSettings.Add(new ShopSetting { Key = key, Value = value });
        else
            setting.Value = value;
    }
}
