using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

/// <summary>
/// Immutable audit record for every sensitive operation in the system.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public AuditActionType Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
