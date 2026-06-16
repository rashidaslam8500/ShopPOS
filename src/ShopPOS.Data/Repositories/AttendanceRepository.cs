using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly PosDbContext _db;
    public AttendanceRepository(PosDbContext db) => _db = db;

    public Task<Attendance?> GetTodayAsync(int workerId, DateTime? date = null)
    {
        var day = (date ?? DateTime.Today).Date;
        return _db.Attendances
            .Include(a => a.Worker)
            .FirstOrDefaultAsync(a => a.WorkerId == workerId && a.Date.Date == day);
    }

    public Task<IReadOnlyList<Attendance>> GetByDateAsync(DateTime date)
    {
        var day = date.Date;
        return _db.Attendances
            .Include(a => a.Worker)
            .Where(a => a.Date.Date == day)
            .OrderBy(a => a.TimeIn)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Attendance>)t.Result);
    }

    public Task<IReadOnlyList<Attendance>> GetByWorkerMonthAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return _db.Attendances
            .Include(a => a.Worker)
            .Where(a => a.WorkerId == workerId && a.Date >= start && a.Date < end)
            .OrderBy(a => a.Date)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Attendance>)t.Result);
    }

    public async Task<Attendance> AddAsync(Attendance attendance)
    {
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync();
        return attendance;
    }

    public async Task UpdateAsync(Attendance attendance)
    {
        _db.Attendances.Update(attendance);
        await _db.SaveChangesAsync();
    }
}
