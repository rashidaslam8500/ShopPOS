using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly PosDbContext _db;
    public AuditRepository(PosDbContext db) => _db = db;

    public async Task LogAsync(AuditLog entry)
    {
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 100, AuditActionType? filter = null)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (filter.HasValue)
            query = query.Where(a => a.Action == filter.Value);

        return await query.OrderByDescending(a => a.Timestamp).Take(count).ToListAsync();
    }

    public async Task DeleteByIdsAsync(IReadOnlyList<long> ids)
    {
        if (ids.Count == 0)
            return;

        var entries = await _db.AuditLogs.Where(a => ids.Contains(a.Id)).ToListAsync();
        if (entries.Count == 0)
            return;

        _db.AuditLogs.RemoveRange(entries);
        await _db.SaveChangesAsync();
    }
}
