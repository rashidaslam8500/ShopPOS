using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface IExpenseAndCashRepository
{
    Task<IReadOnlyList<ExpenseAndCash>> GetAllAsync();
    Task<ExpenseAndCash?> GetByDateAsync(DateTime date);
    Task<ExpenseAndCash> AddAsync(ExpenseAndCash entry);
    Task UpdateAsync(ExpenseAndCash entry);
    Task<decimal> GetTotalExpensesSinceAsync(DateTime since);
    Task<decimal> GetTotalExpensesForDateAsync(DateTime date);
}
