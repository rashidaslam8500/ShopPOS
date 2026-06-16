using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Models;
using ShopPOS.WPF.Services.Reports;

namespace ShopPOS.WPF.ViewModels;

public partial class WorkerProfileViewModel : ViewModelBase
{
    private readonly IWorkerProfileService _profile;
    private readonly IAttendanceService _attendance;
    private readonly ISettingsService _settings;
    private readonly ILedgerPdfReportService _pdfReports;
    private int _workerId;

    [ObservableProperty] private string _workerName = string.Empty;
    [ObservableProperty] private string _workerRole = string.Empty;
    [ObservableProperty] private string _workerPhone = string.Empty;
    [ObservableProperty] private int _profileYear;
    [ObservableProperty] private int _profileMonth;

    [ObservableProperty] private decimal? _dailyCashAmount;
    [ObservableProperty] private string _dailyCashNotes = string.Empty;
    [ObservableProperty] private DateTime? _dailyCashDate = DateTime.Today;

    [ObservableProperty] private decimal? _advanceAmount;
    [ObservableProperty] private string _advanceNotes = string.Empty;
    [ObservableProperty] private DateTime? _advanceDate = DateTime.Today;

    [ObservableProperty] private DateTime? _leaveDate = DateTime.Today;
    [ObservableProperty] private LeaveType _selectedLeaveType = LeaveType.Casual;
    [ObservableProperty] private bool _leaveIsPaid;
    [ObservableProperty] private string _leaveReason = string.Empty;

    [ObservableProperty] private string _salarySheetText = string.Empty;
    [ObservableProperty] private string _monthSummaryText = string.Empty;

    public ObservableCollection<WorkerDailyCash> DailyCashEntries { get; } = new();
    public ObservableCollection<WorkerAdvance> AdvanceEntries { get; } = new();
    public ObservableCollection<WorkerLeave> LeaveEntries { get; } = new();
    public ObservableCollection<Attendance> AttendanceEntries { get; } = new();
    public ObservableCollection<Worker> CheckedInWorkers { get; } = new();

    public Array LeaveTypes => Enum.GetValues(typeof(LeaveType));

    public WorkerProfileViewModel(
        IWorkerProfileService profile,
        IAttendanceService attendance,
        ISettingsService settings,
        ILedgerPdfReportService pdfReports)
    {
        _profile = profile;
        _attendance = attendance;
        _settings = settings;
        _pdfReports = pdfReports;
        _profileYear = DateTime.Today.Year;
        _profileMonth = DateTime.Today.Month;
    }

    public void Initialize(int workerId)
    {
        _workerId = workerId;
        _ = LoadAsync();
    }

    partial void OnProfileYearChanged(int value) => _ = ReloadLedgersAsync();
    partial void OnProfileMonthChanged(int value) => _ = ReloadLedgersAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var worker = await _profile.GetWorkerAsync(_workerId);
            if (worker is null) return;

            WorkerName = worker.Name;
            WorkerRole = worker.Role;
            WorkerPhone = worker.Phone;
            await ReloadLedgersAsync();
            await LoadCheckoutPanelAsync();
        });
    }

    private async Task ReloadLedgersAsync()
    {
        var sheet = await _profile.GetSalarySheetAsync(_workerId, ProfileYear, ProfileMonth);
        SalarySheetText =
            $"Monthly Salary: {_settings.FormatMoney(sheet.MonthlySalary)}\n" +
            $"Overtime ({sheet.OvertimeHours:N1} hrs): {_settings.FormatMoney(sheet.OvertimePay)}\n" +
            $"Advance Deducted: {_settings.FormatMoney(sheet.AdvanceTaken)}\n" +
            $"Daily Cash Deducted: {_settings.FormatMoney(sheet.DailyCashTaken)}\n" +
            $"NET SALARY: {_settings.FormatMoney(sheet.NetSalary)}";

        var summary = await _profile.GetMonthSummaryAsync(_workerId, ProfileYear, ProfileMonth);
        MonthSummaryText =
            $"Present: {summary.PresentDays}  |  Leaves: {summary.LeaveDays}  |  Absent: {summary.AbsentDays}  |  OT Hours: {summary.TotalOvertimeHours:N1}";

        DailyCashEntries.Clear();
        foreach (var x in await _profile.GetDailyCashAsync(_workerId, ProfileYear, ProfileMonth))
            DailyCashEntries.Add(x);

        AdvanceEntries.Clear();
        foreach (var x in await _profile.GetAdvancesAsync(_workerId, ProfileYear, ProfileMonth))
            AdvanceEntries.Add(x);

        LeaveEntries.Clear();
        foreach (var x in await _profile.GetLeavesAsync(_workerId, ProfileYear, ProfileMonth))
            LeaveEntries.Add(x);

        AttendanceEntries.Clear();
        foreach (var x in await _profile.GetAttendanceAsync(_workerId, ProfileYear, ProfileMonth))
            AttendanceEntries.Add(x);
    }

    private async Task LoadCheckoutPanelAsync()
    {
        CheckedInWorkers.Clear();
        var today = await _attendance.GetTodayRecordsAsync();
        foreach (var a in today.Where(x => x.TimeToLeave is null))
        {
            if (a.Worker is not null)
                CheckedInWorkers.Add(a.Worker);
        }
    }

    [RelayCommand]
    private async Task PrintEmployeeReportPdfAsync()
    {
        var safeName = WorkerName.Replace(' ', '_');
        var path = PdfReportExportHelper.PromptSavePath($"EmployeeReport_{safeName}_{ProfileYear}-{ProfileMonth:D2}.pdf");
        if (path is null)
            return;

        await RunSafeAsync(async () =>
        {
            var report = await WorkerReportDataBuilder.BuildAsync(_profile, _settings, _workerId, ProfileYear, ProfileMonth)
                ?? throw new InvalidOperationException("Worker not found.");

            _pdfReports.GenerateWorkerReport(report, path);

            if (PdfReportExportHelper.ConfirmOpenAfterSave())
                PdfReportExportHelper.OpenPdf(path);
        });
    }

    [RelayCommand]
    private async Task AddDailyCashAsync()
    {
        if (!DailyCashDate.HasValue) return;
        await RunSafeAsync(async () =>
        {
            await _profile.AddDailyCashAsync(_workerId, DailyCashDate.Value, DailyCashAmount ?? 0, DailyCashNotes);
            DailyCashAmount = null;
            DailyCashNotes = string.Empty;
            await ReloadLedgersAsync();
        }, "Daily cash recorded.");
    }

    [RelayCommand]
    private async Task AddAdvanceAsync()
    {
        if (!AdvanceDate.HasValue) return;
        await RunSafeAsync(async () =>
        {
            await _profile.AddAdvanceAsync(_workerId, AdvanceDate.Value, AdvanceAmount ?? 0, AdvanceNotes);
            AdvanceAmount = null;
            AdvanceNotes = string.Empty;
            await ReloadLedgersAsync();
        }, "Advance recorded.");
    }

    [RelayCommand]
    private async Task AddLeaveAsync()
    {
        if (!LeaveDate.HasValue) return;
        await RunSafeAsync(async () =>
        {
            await _profile.AddLeaveAsync(_workerId, LeaveDate.Value, SelectedLeaveType, LeaveIsPaid, LeaveReason);
            LeaveReason = string.Empty;
            await ReloadLedgersAsync();
        }, "Leave logged.");
    }

    [RelayCommand]
    private async Task ProcessSalaryAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _profile.ProcessMonthlySalaryAsync(_workerId, ProfileYear, ProfileMonth);
            await ReloadLedgersAsync();
        }, "Salary processed — advances marked settled.");
    }

    [RelayCommand]
    private async Task CheckOutWorkerAsync(Worker? worker)
    {
        if (worker is null) return;
        await RunSafeAsync(async () =>
        {
            var result = await _attendance.MarkByWorkerIdAsync(worker.Id);
            if (!result.Success)
                StatusMessage = result.Message;
            await LoadCheckoutPanelAsync();
            if (worker.Id == _workerId)
                await ReloadLedgersAsync();
        }, "Time out marked.");
    }

    public string FormatMoney(decimal amount) => _settings.FormatMoney(amount);
}
