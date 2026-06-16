using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface IAttendanceRepository
{
    Task<Attendance?> GetTodayAsync(int workerId, DateTime? date = null);
    Task<IReadOnlyList<Attendance>> GetByDateAsync(DateTime date);
    Task<IReadOnlyList<Attendance>> GetByWorkerMonthAsync(int workerId, int year, int month);
    Task<Attendance> AddAsync(Attendance attendance);
    Task UpdateAsync(Attendance attendance);
}
