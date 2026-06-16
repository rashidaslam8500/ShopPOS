using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IWorkerProfileService
{
    Task<Worker?> GetWorkerAsync(int workerId);
    Task<WorkerSalarySheet> GetSalarySheetAsync(int workerId, int year, int month);
    Task<WorkerMonthSummary> GetMonthSummaryAsync(int workerId, int year, int month);
    Task ProcessMonthlySalaryAsync(int workerId, int year, int month);
    Task AddDailyCashAsync(int workerId, DateTime date, decimal amount, string? notes);
    Task AddAdvanceAsync(int workerId, DateTime date, decimal amount, string? notes);
    Task AddLeaveAsync(int workerId, DateTime date, LeaveType leaveType, bool isPaid, string? reason);
    Task<IReadOnlyList<WorkerDailyCash>> GetDailyCashAsync(int workerId, int year, int month);
    Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(int workerId, int year, int month);
    Task<IReadOnlyList<WorkerLeave>> GetLeavesAsync(int workerId, int year, int month);
    Task<IReadOnlyList<Attendance>> GetAttendanceAsync(int workerId, int year, int month);
}

public class WorkerProfileService : IWorkerProfileService
{
    private readonly IWorkerProfileRepository _profile;
    private readonly IWorkerRepository _workers;
    private readonly IAttendanceRepository _attendance;

    public WorkerProfileService(
        IWorkerProfileRepository profile,
        IWorkerRepository workers,
        IAttendanceRepository attendance)
    {
        _profile = profile;
        _workers = workers;
        _attendance = attendance;
    }

    public Task<Worker?> GetWorkerAsync(int workerId) => _profile.GetWorkerAsync(workerId);

    public async Task<WorkerSalarySheet> GetSalarySheetAsync(int workerId, int year, int month)
    {
        var worker = await _profile.GetWorkerAsync(workerId)
            ?? throw new InvalidOperationException("Worker not found.");

        var summary = await GetMonthSummaryAsync(workerId, year, month);
        var advanceTaken = await _profile.SumUnsettledAdvancesAsync(workerId, year, month);
        var dailyCashTaken = await _profile.SumDailyCashAsync(workerId, year, month);
        var overtimePay = summary.TotalOvertimeHours * worker.HourlyOvertimeRate;
        var net = worker.MonthlySalary + overtimePay - advanceTaken - dailyCashTaken;

        return new WorkerSalarySheet
        {
            WorkerId = workerId,
            WorkerName = worker.Name,
            Year = year,
            Month = month,
            MonthlySalary = worker.MonthlySalary,
            OvertimeHours = summary.TotalOvertimeHours,
            OvertimePay = overtimePay,
            AdvanceTaken = advanceTaken,
            DailyCashTaken = dailyCashTaken,
            NetSalary = net,
            PresentDays = summary.PresentDays,
            LeaveDays = summary.LeaveDays,
            AbsentDays = summary.AbsentDays
        };
    }

    public async Task<WorkerMonthSummary> GetMonthSummaryAsync(int workerId, int year, int month)
    {
        var attendance = await _attendance.GetByWorkerMonthAsync(workerId, year, month);
        var leaves = await _profile.GetLeavesAsync(workerId, year, month);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var presentDays = attendance.Count(a => a.Status == AttendanceStatus.Present);
        var leaveDays = leaves.Count;
        var absentDays = Math.Max(0, daysInMonth - presentDays - leaveDays);
        var otHours = attendance.Sum(a => a.OvertimeHours);

        return new WorkerMonthSummary
        {
            PresentDays = presentDays,
            LeaveDays = leaveDays,
            AbsentDays = absentDays,
            TotalOvertimeHours = otHours
        };
    }

    public async Task ProcessMonthlySalaryAsync(int workerId, int year, int month)
    {
        await _profile.SettleAdvancesAsync(workerId, year, month);
    }

    public Task AddDailyCashAsync(int workerId, DateTime date, decimal amount, string? notes) =>
        _profile.AddDailyCashAsync(new WorkerDailyCash
        {
            WorkerId = workerId,
            Date = date.Date,
            Amount = amount,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

    public Task AddAdvanceAsync(int workerId, DateTime date, decimal amount, string? notes) =>
        _profile.AddAdvanceAsync(new WorkerAdvance
        {
            WorkerId = workerId,
            Date = date.Date,
            Amount = amount,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

    public Task AddLeaveAsync(int workerId, DateTime date, LeaveType leaveType, bool isPaid, string? reason) =>
        _profile.AddLeaveAsync(new WorkerLeave
        {
            WorkerId = workerId,
            Date = date.Date,
            LeaveType = leaveType,
            IsPaid = isPaid,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

    public Task<IReadOnlyList<WorkerDailyCash>> GetDailyCashAsync(int workerId, int year, int month) =>
        _profile.GetDailyCashAsync(workerId, year, month);

    public Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(int workerId, int year, int month) =>
        _profile.GetAdvancesAsync(workerId, year, month);

    public Task<IReadOnlyList<WorkerLeave>> GetLeavesAsync(int workerId, int year, int month) =>
        _profile.GetLeavesAsync(workerId, year, month);

    public Task<IReadOnlyList<Attendance>> GetAttendanceAsync(int workerId, int year, int month) =>
        _attendance.GetByWorkerMonthAsync(workerId, year, month);
}

public interface IOwnerPersonalExpenseService
{
    Task<IReadOnlyList<OwnerPersonalExpense>> SearchAsync(string? query, DateTime? from, DateTime? to);
    Task AddAsync(DateTime date, decimal amount, OwnerExpenseCategory category, string description);
    Task<decimal> GetTotalAllTimeAsync();
}

public class OwnerPersonalExpenseService : IOwnerPersonalExpenseService
{
    private readonly IOwnerPersonalExpenseRepository _repo;
    private readonly IAuditService _audit;

    public OwnerPersonalExpenseService(IOwnerPersonalExpenseRepository repo, IAuditService audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public Task<IReadOnlyList<OwnerPersonalExpense>> SearchAsync(string? query, DateTime? from, DateTime? to) =>
        _repo.SearchAsync(query, from, to);

    public async Task AddAsync(DateTime date, decimal amount, OwnerExpenseCategory category, string description)
    {
        await _repo.AddAsync(new OwnerPersonalExpense
        {
            Date = date.Date,
            Amount = amount,
            Category = category,
            Description = description.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _audit.LogAsync(AuditActionType.ExpenseLogged, "OwnerPersonalExpense", date.ToString("yyyy-MM-dd"),
            $"{category}: Rs.{amount:N2}");
    }

    public Task<decimal> GetTotalAllTimeAsync() =>
        _repo.GetTotalSinceAsync(DateTime.MinValue);
}

public interface IVendorKhataService
{
    Task<IReadOnlyList<Vendor>> GetVendorsAsync();
    Task<Vendor> AddVendorAsync(string name, string phone, string? address);
    Task UpdateVendorAsync(int vendorId, string name, string phone, string? address);
    Task<VendorKhataSummary> GetSummaryAsync(int vendorId);
    Task<IReadOnlyList<VendorKhataLine>> GetKhataLinesAsync(int vendorId);
    Task<IReadOnlyList<VendorKhataLine>> GetTrashedLinesAsync(int vendorId);
    Task AddKhataEntryAsync(int vendorId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes);
    Task<int> AddKhataEntryReturnIdAsync(int vendorId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes);
    Task UpdateKhataEntryAsync(int entryId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes);
    Task SoftDeleteKhataEntryAsync(int entryId);
    Task RestoreKhataEntryAsync(int entryId);
    Task<IReadOnlyList<string?>> PermanentDeleteKhataEntriesAsync(IEnumerable<int> entryIds);
    Task SetAttachmentPathAsync(int entryId, string? attachmentPath);
    Task<VendorKhataEntry?> GetEntryAsync(int entryId);
}

public class VendorKhataService : IVendorKhataService
{
    private readonly IVendorRepository _vendors;

    public VendorKhataService(IVendorRepository vendors) => _vendors = vendors;

    public Task<IReadOnlyList<Vendor>> GetVendorsAsync() => _vendors.GetAllAsync();

    public Task<Vendor> AddVendorAsync(string name, string phone, string? address) =>
        _vendors.AddAsync(new Vendor
        {
            Name = name.Trim(),
            Phone = phone.Trim(),
            Address = address?.Trim(),
            CreatedAt = DateTime.UtcNow
        });

    public async Task UpdateVendorAsync(int vendorId, string name, string phone, string? address)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId)
            ?? throw new InvalidOperationException("Vendor not found.");
        vendor.Name = name.Trim();
        vendor.Phone = phone.Trim();
        vendor.Address = address?.Trim();
        await _vendors.UpdateAsync(vendor);
    }

    public async Task<VendorKhataSummary> GetSummaryAsync(int vendorId)
    {
        var entries = await _vendors.GetKhataEntriesAsync(vendorId);
        var totalDues = entries.Sum(e => e.TotalBill);
        var totalPaid = entries.Sum(e => e.CashPaid);
        return new VendorKhataSummary
        {
            TotalDues = totalDues,
            TotalCashPaid = totalPaid,
            CurrentNetBalance = totalDues - totalPaid
        };
    }

    public async Task<IReadOnlyList<VendorKhataLine>> GetKhataLinesAsync(int vendorId)
    {
        var entries = await _vendors.GetKhataEntriesAsync(vendorId);
        return BuildRunningLines(entries);
    }

    public async Task<IReadOnlyList<VendorKhataLine>> GetTrashedLinesAsync(int vendorId)
    {
        var entries = await _vendors.GetDeletedKhataEntriesAsync(vendorId);
        return entries.Select(MapLine).ToList();
    }

    public Task AddKhataEntryAsync(int vendorId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes) =>
        _vendors.AddKhataEntryAsync(new VendorKhataEntry
        {
            VendorId = vendorId,
            Date = date.Date,
            InvoiceNumber = invoiceNo.Trim(),
            TotalBill = totalBill,
            CashPaid = cashPaid,
            PaymentMode = paymentMode,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

    public Task<int> AddKhataEntryReturnIdAsync(int vendorId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes) =>
        _vendors.AddKhataEntryReturnIdAsync(new VendorKhataEntry
        {
            VendorId = vendorId,
            Date = date.Date,
            InvoiceNumber = invoiceNo.Trim(),
            TotalBill = totalBill,
            CashPaid = cashPaid,
            PaymentMode = paymentMode,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        });

    public async Task UpdateKhataEntryAsync(int entryId, DateTime date, string invoiceNo, decimal totalBill, decimal cashPaid, VendorKhataPaymentMode paymentMode, string? notes)
    {
        var entry = await _vendors.GetKhataEntryByIdAsync(entryId)
            ?? throw new InvalidOperationException("Khata entry not found.");
        if (entry.IsDeleted)
            throw new InvalidOperationException("Cannot edit an entry in the Trash Bin. Restore it first.");

        entry.Date = date.Date;
        entry.InvoiceNumber = invoiceNo.Trim();
        entry.TotalBill = totalBill;
        entry.CashPaid = cashPaid;
        entry.PaymentMode = paymentMode;
        entry.Notes = notes;
        await _vendors.UpdateKhataEntryAsync(entry);
    }

    public Task SoftDeleteKhataEntryAsync(int entryId) =>
        _vendors.SoftDeleteKhataEntryAsync(entryId);

    public async Task RestoreKhataEntryAsync(int entryId)
    {
        var entry = await _vendors.GetKhataEntryByIdAsync(entryId)
            ?? throw new InvalidOperationException("Khata entry not found.");
        if (!entry.IsDeleted)
            throw new InvalidOperationException("This entry is already active. Only trashed entries can be restored.");

        await _vendors.RestoreKhataEntryAsync(entryId);
    }

    public async Task<IReadOnlyList<string?>> PermanentDeleteKhataEntriesAsync(IEnumerable<int> entryIds)
    {
        var ids = entryIds.Distinct().ToList();
        var paths = new List<string?>();
        foreach (var id in ids)
        {
            var entry = await _vendors.GetKhataEntryByIdAsync(id);
            if (entry is not null && entry.IsDeleted)
                paths.Add(entry.AttachmentPath);
        }

        await _vendors.PermanentDeleteKhataEntriesAsync(ids);
        return paths;
    }

    public Task SetAttachmentPathAsync(int entryId, string? attachmentPath) =>
        _vendors.SetAttachmentPathAsync(entryId, attachmentPath);

    public Task<VendorKhataEntry?> GetEntryAsync(int entryId) =>
        _vendors.GetKhataEntryByIdAsync(entryId);

    private static IReadOnlyList<VendorKhataLine> BuildRunningLines(IReadOnlyList<VendorKhataEntry> entries)
    {
        decimal running = 0;
        var lines = new List<VendorKhataLine>();
        foreach (var e in entries)
        {
            var previous = running;
            running += e.TotalBill - e.CashPaid;
            lines.Add(new VendorKhataLine
            {
                Id = e.Id,
                Date = e.Date,
                InvoiceNumber = e.InvoiceNumber,
                Description = e.Notes,
                PreviousBalance = previous,
                TotalBill = e.TotalBill,
                CashPaid = e.CashPaid,
                RunningBalance = running,
                PaymentMode = e.PaymentMode,
                Notes = e.Notes,
                AttachmentPath = e.AttachmentPath,
                HasAttachment = !string.IsNullOrWhiteSpace(e.AttachmentPath)
            });
        }
        return lines;
    }

    private static VendorKhataLine MapLine(VendorKhataEntry e) => new()
    {
        Id = e.Id,
        Date = e.Date,
        InvoiceNumber = e.InvoiceNumber,
        Description = e.Notes,
        TotalBill = e.TotalBill,
        CashPaid = e.CashPaid,
        PaymentMode = e.PaymentMode,
        Notes = e.Notes,
        AttachmentPath = e.AttachmentPath,
        HasAttachment = !string.IsNullOrWhiteSpace(e.AttachmentPath),
        DeletedAt = e.DeletedAt
    };
}
