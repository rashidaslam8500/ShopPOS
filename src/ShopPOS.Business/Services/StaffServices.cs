using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IWorkerService
{
    Task<IReadOnlyList<Worker>> GetWorkersAsync();
    Task<Worker> AddWorkerAsync(Worker worker);
    Task SoftDeleteWorkerAsync(int workerId);
}

public class WorkerService : IWorkerService
{
    private readonly IWorkerRepository _workers;
    private readonly IAuditService _audit;

    public WorkerService(IWorkerRepository workers, IAuditService audit)
    {
        _workers = workers;
        _audit = audit;
    }

    public Task<IReadOnlyList<Worker>> GetWorkersAsync() => _workers.GetAllAsync();

    public async Task<Worker> AddWorkerAsync(Worker worker)
    {
        if (string.IsNullOrWhiteSpace(worker.Name))
            throw new ArgumentException("Worker name is required.");

        worker.CreatedAt = DateTime.UtcNow;
        worker.IsActive = true;
        if (worker.HourlyOvertimeRate <= 0 && worker.MonthlySalary > 0)
        {
            var shiftHours = worker.StandardShiftHours > 0 ? worker.StandardShiftHours : 8;
            worker.HourlyOvertimeRate = Math.Round(worker.MonthlySalary / (26m * shiftHours), 2);
        }
        var saved = await _workers.AddAsync(worker);
        await _audit.LogAsync(AuditActionType.WorkerAdded, "Worker", saved.Id.ToString(), $"Added worker {saved.Name}");
        return saved;
    }

    public async Task SoftDeleteWorkerAsync(int workerId)
    {
        var worker = await _workers.GetByIdAsync(workerId)
            ?? throw new InvalidOperationException("Worker not found.");

        worker.IsActive = false;
        await _workers.UpdateAsync(worker);
        await _audit.LogAsync(AuditActionType.WorkerDeleted, "Worker", workerId.ToString(), $"Deactivated worker {worker.Name}");
    }
}

public interface IAttendanceService
{
    Task<IReadOnlyList<Attendance>> GetTodayRecordsAsync();
    Task<AttendanceMarkResult> MarkByFingerprintAsync(string templateBase64);
    Task<AttendanceMarkResult> MarkByWorkerIdAsync(int workerId);
}

public class AttendanceService : IAttendanceService
{
    private readonly IWorkerRepository _workers;
    private readonly IAttendanceRepository _attendance;
    private readonly IFingerprintScannerService _fingerprint;

    public AttendanceService(
        IWorkerRepository workers,
        IAttendanceRepository attendance,
        IFingerprintScannerService fingerprint)
    {
        _workers = workers;
        _attendance = attendance;
        _fingerprint = fingerprint;
    }

    public Task<IReadOnlyList<Attendance>> GetTodayRecordsAsync() =>
        _attendance.GetByDateAsync(DateTime.Today);

    public async Task<AttendanceMarkResult> MarkByFingerprintAsync(string templateBase64)
    {
        var workerId = await _fingerprint.IdentifyWorkerAsync(templateBase64);
        if (workerId is null)
            return new AttendanceMarkResult { Success = false, Message = "Fingerprint not recognized." };

        return await MarkByWorkerIdAsync(workerId.Value);
    }

    public async Task<AttendanceMarkResult> MarkByWorkerIdAsync(int workerId)
    {
        var worker = await _workers.GetByIdAsync(workerId);
        if (worker is null)
            return Fail("Worker not found.");

        var today = await _attendance.GetTodayAsync(workerId);
        var now = DateTime.Now.TimeOfDay;

        if (today is null)
        {
            var record = new Attendance
            {
                WorkerId = workerId,
                Date = DateTime.Today,
                TimeIn = now,
                Status = AttendanceStatus.Present
            };
            await _attendance.AddAsync(record);
            return new AttendanceMarkResult
            {
                Success = true,
                WorkerId = workerId,
                WorkerName = worker.Name,
                IsTimeIn = true,
                Message = $"Welcome {worker.Name}! Time In Marked"
            };
        }

        if (today.TimeToLeave is null)
        {
            today.TimeToLeave = now;
            var worked = today.TimeToLeave.Value - today.TimeIn;
            var standard = TimeSpan.FromHours((double)Math.Max(1, worker.StandardShiftHours));
            if (worked > standard)
                today.OvertimeHours = Math.Round((decimal)(worked - standard).TotalHours, 2);

            await _attendance.UpdateAsync(today);
            return new AttendanceMarkResult
            {
                Success = true,
                WorkerId = workerId,
                WorkerName = worker.Name,
                IsTimeIn = false,
                Message = $"Goodbye {worker.Name}! Time Out Marked"
            };
        }

        return Fail($"{worker.Name} has already completed today's attendance.");
    }

    private static AttendanceMarkResult Fail(string message) =>
        new() { Success = false, Message = message };
}

public interface IExpenseAndCashService
{
    Task<IReadOnlyList<ExpenseAndCash>> GetEntriesAsync();
    Task SaveEntryAsync(DateTime date, decimal dailyCashIntake, decimal totalExpense, string description);
    Task<decimal> GetTodayExpensesAsync();
    Task<decimal> GetAllTimeExpensesAsync();
}

public class ExpenseAndCashService : IExpenseAndCashService
{
    private readonly IExpenseAndCashRepository _expenses;
    private readonly IAuditService _audit;

    public ExpenseAndCashService(IExpenseAndCashRepository expenses, IAuditService audit)
    {
        _expenses = expenses;
        _audit = audit;
    }

    public Task<IReadOnlyList<ExpenseAndCash>> GetEntriesAsync() => _expenses.GetAllAsync();

    public async Task SaveEntryAsync(DateTime date, decimal dailyCashIntake, decimal totalExpense, string description)
    {
        var existing = await _expenses.GetByDateAsync(date);
        if (existing is null)
        {
            await _expenses.AddAsync(new ExpenseAndCash
            {
                Date = date.Date,
                DailyCashIntake = dailyCashIntake,
                TotalExpense = totalExpense,
                ExpenseDescription = description.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.DailyCashIntake = dailyCashIntake;
            existing.TotalExpense = totalExpense;
            existing.ExpenseDescription = description.Trim();
            await _expenses.UpdateAsync(existing);
        }

        await _audit.LogAsync(AuditActionType.ExpenseLogged, "ExpenseAndCash", date.ToString("yyyy-MM-dd"),
            $"Cash {dailyCashIntake:N2}, Expense {totalExpense:N2}");
    }

    public Task<decimal> GetTodayExpensesAsync() =>
        _expenses.GetTotalExpensesForDateAsync(DateTime.Today);

    public Task<decimal> GetAllTimeExpensesAsync() =>
        _expenses.GetTotalExpensesSinceAsync(DateTime.MinValue);
}
