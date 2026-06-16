using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class ExpenseAndCashRepository : IExpenseAndCashRepository
{
    private readonly PosDbContext _db;
    public ExpenseAndCashRepository(PosDbContext db) => _db = db;

    public Task<IReadOnlyList<ExpenseAndCash>> GetAllAsync() =>
        _db.ExpenseAndCashEntries
            .OrderByDescending(e => e.Date)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<ExpenseAndCash>)t.Result);

    public Task<ExpenseAndCash?> GetByDateAsync(DateTime date) =>
        _db.ExpenseAndCashEntries.FirstOrDefaultAsync(e => e.Date.Date == date.Date);

    public async Task<ExpenseAndCash> AddAsync(ExpenseAndCash entry)
    {
        _db.ExpenseAndCashEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateAsync(ExpenseAndCash entry)
    {
        _db.ExpenseAndCashEntries.Update(entry);
        await _db.SaveChangesAsync();
    }

    public Task<decimal> GetTotalExpensesSinceAsync(DateTime since) =>
        _db.ExpenseAndCashEntries
            .Where(e => e.Date >= since)
            .SumAsync(e => e.TotalExpense);

    public Task<decimal> GetTotalExpensesForDateAsync(DateTime date) =>
        _db.ExpenseAndCashEntries
            .Where(e => e.Date.Date == date.Date)
            .SumAsync(e => e.TotalExpense);
}
