using ShopPOS.Data.Security;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Business.Services;

public interface IAuditService
{
    Task LogAsync(AuditActionType action, string entityType, string? entityId, string details, string? oldValues = null, string? newValues = null);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 200, AuditActionType? filter = null);
    Task DeleteLogsAsync(IReadOnlyList<long> ids, string ownerPassword);
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;
    private readonly IUserRepository _users;
    private readonly CurrentSession _session;

    public AuditService(IAuditRepository audit, IUserRepository users, CurrentSession session)
    {
        _audit = audit;
        _users = users;
        _session = session;
    }

    public Task LogAsync(AuditActionType action, string entityType, string? entityId, string details, string? oldValues = null, string? newValues = null)
    {
        var entry = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = _session.User?.UserId,
            Username = _session.User?.Username ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            OldValues = oldValues,
            NewValues = newValues
        };
        return _audit.LogAsync(entry);
    }

    public Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 200, AuditActionType? filter = null) =>
        _audit.GetRecentAsync(count, filter);

    public async Task DeleteLogsAsync(IReadOnlyList<long> ids, string ownerPassword)
    {
        if (ids.Count == 0)
            throw new InvalidOperationException("Select at least one audit log entry.");

        if (string.IsNullOrWhiteSpace(ownerPassword))
            throw new InvalidOperationException("Enter the owner password.");

        var owner = await _users.GetByUsernameAsync("owner");
        if (owner is null || !PasswordHasher.Verify(ownerPassword, owner.PasswordHash, owner.PasswordSalt))
            throw new UnauthorizedAccessException("Owner password is incorrect.");

        await _audit.DeleteByIdsAsync(ids);

        var details = ids.Count == 1
            ? $"Deleted audit log entry #{ids[0]}"
            : $"Deleted {ids.Count} audit log entries: {string.Join(", ", ids)}";

        var entityId = ids.Count == 1
            ? ids[0].ToString()
            : FormatAuditEntityId(ids);

        await LogAsync(AuditActionType.AuditLogDeleted, "AuditLog", entityId, details);
    }

    private static string FormatAuditEntityId(IReadOnlyList<long> ids)
    {
        var joined = string.Join(",", ids);
        if (joined.Length <= 50)
            return joined;

        return ids.Count <= 9999
            ? $"{ids.Count} selected"
            : "bulk";
    }
}
