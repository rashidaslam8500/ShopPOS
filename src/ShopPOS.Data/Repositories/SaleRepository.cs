using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly PosDbContext _db;
    public SaleRepository(PosDbContext db) => _db = db;

    private IQueryable<Sale> WithDetails(bool includeDeleted) =>
        WithDetailsQuery(includeDeleted);

    private IQueryable<Sale> WithDetailsQuery(bool includeDeleted)
    {
        var query = _db.Sales
            .Include(s => s.Items)
            .Include(s => s.Returns)
            .Include(s => s.Amendments)
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(s => !s.IsDeleted);

        return query;
    }

    public async Task<IReadOnlyList<Sale>> GetAllAsync(DateTime? date = null, bool includeDeleted = false)
    {
        var query = WithDetailsQuery(includeDeleted);
        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(s => s.SaleDate >= start && s.SaleDate < end);
        }
        return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
    }

    public Task<IReadOnlyList<Sale>> GetDeletedAsync() =>
        _db.Sales
            .Include(s => s.Items)
            .Where(s => s.IsDeleted)
            .OrderByDescending(s => s.DeletedAt)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Sale>)t.Result);

    public Task<Sale?> GetByIdAsync(int id, bool includeDeleted = false) =>
        WithDetailsQuery(includeDeleted).FirstOrDefaultAsync(s => s.Id == id);

    public Task<Sale?> GetByReceiptNoAsync(string receiptNo, bool includeDeleted = false) =>
        WithDetailsQuery(includeDeleted).FirstOrDefaultAsync(s => s.ReceiptNo == receiptNo);

    public async Task<Sale> AddAsync(Sale sale)
    {
        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();
        return sale;
    }

    public async Task UpdateAsync(Sale sale)
    {
        _db.Sales.Update(sale);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetNextReceiptSequenceAsync()
    {
        var start = DateTime.Now.Date;
        var end = start.AddDays(1);
        var prefix = $"BG{DateTime.Now:yyyyMMdd}-";
        var receipts = await _db.Sales
            .Where(s => s.SaleDate >= start && s.SaleDate < end && s.ReceiptNo.StartsWith(prefix))
            .Select(s => s.ReceiptNo)
            .ToListAsync();

        var max = 0;
        foreach (var receipt in receipts)
        {
            if (receipt.Length <= prefix.Length)
                continue;

            if (int.TryParse(receipt[prefix.Length..], out var sequence))
                max = Math.Max(max, sequence);
        }

        return max + 1;
    }

    public async Task<IReadOnlyList<Sale>> GetSalesSinceAsync(DateTime sinceUtc)
    {
        var sinceLocal = sinceUtc.ToLocalTime();
        return await WithDetailsQuery(false)
            .Where(s => s.SaleDate > sinceLocal)
            .OrderBy(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task PermanentDeleteAsync(int saleId)
    {
        var sale = await WithDetailsQuery(true).FirstOrDefaultAsync(s => s.Id == saleId && s.IsDeleted);
        if (sale is null)
            return;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var line in sale.Items)
                {
                    var restoreQty = line.IsAmendmentLine
                        ? line.Quantity
                        : line.Quantity - line.ReturnedQuantity;

                    if (restoreQty <= 0)
                        continue;

                    var product = await _db.Products.FindAsync(line.ProductId);
                    if (product is not null)
                        product.Stock += restoreQty;
                }

                await _db.SaveChangesAsync();

                await _db.SaleReturns.Where(r => r.SaleId == saleId).ExecuteDeleteAsync();
                await _db.SaleAmendments.Where(a => a.SaleId == saleId).ExecuteDeleteAsync();
                await _db.SaleItems.Where(i => i.SaleId == saleId).ExecuteDeleteAsync();
                await _db.Sales.Where(s => s.Id == saleId).ExecuteDeleteAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task RunInTransactionAsync(Func<Task> work)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await work();
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public Task StageReturnAsync(SaleReturn saleReturn)
    {
        _db.SaleReturns.Add(saleReturn);
        return Task.CompletedTask;
    }

    public Task StageAmendmentAsync(SaleAmendment amendment)
    {
        _db.SaleAmendments.Add(amendment);
        return Task.CompletedTask;
    }

    public Task StageItemAsync(SaleItem item)
    {
        _db.SaleItems.Add(item);
        return Task.CompletedTask;
    }
}
