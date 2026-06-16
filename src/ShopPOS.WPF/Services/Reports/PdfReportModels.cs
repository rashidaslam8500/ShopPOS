namespace ShopPOS.WPF.Services.Reports;

public sealed class VendorLedgerReportData
{
    public required string ShopTitle { get; init; }
    public required string ShopContactLine { get; init; }
    public required DateTime ExportDate { get; init; }
    public required string VendorName { get; init; }
    public required string VendorPhone { get; init; }
    public required string VendorAddress { get; init; }
    public required string TotalBillsText { get; init; }
    public required string TotalPaidText { get; init; }
    public required string RemainingBalanceText { get; init; }
    public required IReadOnlyList<VendorLedgerReportLine> Lines { get; init; }
}

public sealed class VendorLedgerReportLine
{
    public required string DateText { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string Description { get; init; }
    public required string PreviousBalanceText { get; init; }
    public required string TotalBillText { get; init; }
    public required string CashPaidText { get; init; }
    public required string RemainingBalanceText { get; init; }
}

public sealed class WorkerEmployeeReportData
{
    public required string ShopTitle { get; init; }
    public required string ShopContactLine { get; init; }
    public required DateTime ExportDate { get; init; }
    public required int ReportYear { get; init; }
    public required int ReportMonth { get; init; }
    public required string WorkerName { get; init; }
    public required string Designation { get; init; }
    public required string JoiningDateText { get; init; }
    public required string ContactPhone { get; init; }
    public required string ThumbScannerStatus { get; init; }
    public required string PresentDaysText { get; init; }
    public required string AbsentDaysText { get; init; }
    public required string LeaveDaysText { get; init; }
    public required string TotalOvertimeHoursText { get; init; }
    public required string TotalAdvanceText { get; init; }
    public required string FinalPayableSalaryText { get; init; }
    public required IReadOnlyList<WorkerEmployeeReportLine> DetailLines { get; init; }
}

public sealed class WorkerEmployeeReportLine
{
    public required string DateText { get; init; }
    public required string EntryType { get; init; }
    public required string Description { get; init; }
    public required string TimeInText { get; init; }
    public required string TimeOutText { get; init; }
    public required string OvertimeHoursText { get; init; }
    public required string AmountText { get; init; }
}
