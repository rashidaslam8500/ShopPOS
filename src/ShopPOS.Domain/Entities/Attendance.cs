using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan TimeIn { get; set; }
    public TimeSpan? TimeToLeave { get; set; }
    public decimal OvertimeHours { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public Worker Worker { get; set; } = null!;
}
