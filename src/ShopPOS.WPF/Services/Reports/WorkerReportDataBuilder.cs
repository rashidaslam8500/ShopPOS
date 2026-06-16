using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;
using ShopPOS.WPF.Services.Reports;

namespace ShopPOS.WPF.Services.Reports;

public static class WorkerReportDataBuilder
{
    public static async Task<WorkerEmployeeReportData?> BuildAsync(
        IWorkerProfileService profile,
        ISettingsService settings,
        int workerId,
        int year,
        int month)
    {
        var worker = await profile.GetWorkerAsync(workerId);
        if (worker is null)
            return null;

        var config = await settings.GetConfigAsync();
        var sheet = await profile.GetSalarySheetAsync(workerId, year, month);
        var summary = await profile.GetMonthSummaryAsync(workerId, year, month);
        var attendance = await profile.GetAttendanceAsync(workerId, year, month);
        var advances = await profile.GetAdvancesAsync(workerId, year, month);
        var dailyCash = await profile.GetDailyCashAsync(workerId, year, month);
        var leaves = await profile.GetLeavesAsync(workerId, year, month);

        var detailLines = new List<WorkerEmployeeReportLine>();

        foreach (var record in attendance.OrderBy(x => x.Date))
        {
            detailLines.Add(new WorkerEmployeeReportLine
            {
                DateText = record.Date.ToString("dd/MM/yyyy"),
                EntryType = "Attendance",
                Description = record.Status.ToString(),
                TimeInText = record.TimeIn.ToString(@"hh\:mm"),
                TimeOutText = record.TimeToLeave?.ToString(@"hh\:mm") ?? "—",
                OvertimeHoursText = record.OvertimeHours > 0 ? record.OvertimeHours.ToString("N1") : "—",
                AmountText = "—"
            });
        }

        foreach (var leave in leaves.OrderBy(x => x.Date))
        {
            detailLines.Add(new WorkerEmployeeReportLine
            {
                DateText = leave.Date.ToString("dd/MM/yyyy"),
                EntryType = "Leave",
                Description = $"{leave.LeaveType} ({(leave.IsPaid ? "Paid" : "Unpaid")}) — {leave.Reason ?? "—"}",
                TimeInText = "—",
                TimeOutText = "—",
                OvertimeHoursText = "—",
                AmountText = "—"
            });
        }

        foreach (var advance in advances.OrderBy(x => x.Date))
        {
            detailLines.Add(new WorkerEmployeeReportLine
            {
                DateText = advance.Date.ToString("dd/MM/yyyy"),
                EntryType = "Advance",
                Description = string.IsNullOrWhiteSpace(advance.Notes) ? "Advance payment" : advance.Notes.Trim(),
                TimeInText = "—",
                TimeOutText = "—",
                OvertimeHoursText = "—",
                AmountText = PdfReportFormatting.FormatAmount(advance.Amount)
            });
        }

        foreach (var cash in dailyCash.OrderBy(x => x.Date))
        {
            detailLines.Add(new WorkerEmployeeReportLine
            {
                DateText = cash.Date.ToString("dd/MM/yyyy"),
                EntryType = "Daily Cash",
                Description = string.IsNullOrWhiteSpace(cash.Notes) ? "Daily cash intake" : cash.Notes.Trim(),
                TimeInText = "—",
                TimeOutText = "—",
                OvertimeHoursText = "—",
                AmountText = PdfReportFormatting.FormatAmount(cash.Amount)
            });
        }

        detailLines.Sort((a, b) =>
        {
            var dateCompare = ParseDate(a.DateText).CompareTo(ParseDate(b.DateText));
            return dateCompare != 0 ? dateCompare : string.Compare(a.EntryType, b.EntryType, StringComparison.Ordinal);
        });

        return new WorkerEmployeeReportData
        {
            ShopTitle = BrandDisplay.TitleLine,
            ShopContactLine = $"{config.Address}  |  {config.Phone}",
            ExportDate = DateTime.Now,
            ReportYear = year,
            ReportMonth = month,
            WorkerName = worker.Name,
            Designation = worker.Role,
            JoiningDateText = worker.CreatedAt.ToLocalTime().ToString("dd MMM yyyy"),
            ContactPhone = worker.Phone,
            ThumbScannerStatus = string.IsNullOrWhiteSpace(worker.FingerprintTemplate) ? "Not Registered" : "Registered",
            PresentDaysText = summary.PresentDays.ToString(),
            AbsentDaysText = summary.AbsentDays.ToString(),
            LeaveDaysText = summary.LeaveDays.ToString(),
            TotalOvertimeHoursText = summary.TotalOvertimeHours.ToString("N1"),
            TotalAdvanceText = PdfReportFormatting.FormatAmount(sheet.AdvanceTaken),
            FinalPayableSalaryText = PdfReportFormatting.FormatAmount(sheet.NetSalary),
            DetailLines = detailLines
        };
    }

    private static DateTime ParseDate(string text) =>
        DateTime.TryParseExact(text, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
}
