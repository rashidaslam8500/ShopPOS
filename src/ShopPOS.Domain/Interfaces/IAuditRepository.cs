using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Interfaces;

public interface IAuditRepository
{
    Task LogAsync(AuditLog entry);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 100, AuditActionType? filter = null);
    Task DeleteByIdsAsync(IReadOnlyList<long> ids);
}
