using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class WorkerLeave
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public LeaveType LeaveType { get; set; }
    public bool IsPaid { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Worker Worker { get; set; } = null!;
}
