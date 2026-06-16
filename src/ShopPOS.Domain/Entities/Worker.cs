namespace ShopPOS.Domain.Entities;

public class Worker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public decimal StandardShiftHours { get; set; } = 8m;
    public decimal HourlyOvertimeRate { get; set; }
    public string? FingerprintTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<WorkerDailyCash> DailyCashRecords { get; set; } = new List<WorkerDailyCash>();
    public ICollection<WorkerAdvance> AdvanceRecords { get; set; } = new List<WorkerAdvance>();
    public ICollection<WorkerLeave> LeaveRecords { get; set; } = new List<WorkerLeave>();
}
