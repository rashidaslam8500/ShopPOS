using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Models;

public class WorkerSalarySheet
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal AdvanceTaken { get; set; }
    public decimal DailyCashTaken { get; set; }
    public decimal NetSalary { get; set; }
    public int PresentDays { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
}

public class WorkerMonthSummary
{
    public int PresentDays { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
}

public class VendorKhataSummary
{
    public decimal TotalDues { get; set; }
    public decimal TotalCashPaid { get; set; }
    public decimal CurrentNetBalance { get; set; }
}

public class VendorKhataLine
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal TotalBill { get; set; }
    public decimal CashPaid { get; set; }
    public decimal RunningBalance { get; set; }
    public VendorKhataPaymentMode PaymentMode { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool HasAttachment { get; set; }
    public DateTime? DeletedAt { get; set; }
}
