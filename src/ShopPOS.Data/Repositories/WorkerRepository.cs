using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class WorkerRepository : IWorkerRepository
{
    private readonly PosDbContext _db;
    public WorkerRepository(PosDbContext db) => _db = db;

    public async Task<IReadOnlyList<Worker>> GetAllAsync(bool includeInactive = false)
    {
        var query = _db.Workers.AsQueryable();
        if (!includeInactive)
            query = query.Where(w => w.IsActive);
        return await query.OrderBy(w => w.Name).ToListAsync();
    }

    public Task<Worker?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var query = _db.Workers.AsQueryable();
        if (!includeInactive)
            query = query.Where(w => w.IsActive);
        return query.FirstOrDefaultAsync(w => w.Id == id);
    }

    public Task<Worker?> GetByFingerprintTemplateAsync(string template) =>
        _db.Workers.FirstOrDefaultAsync(w => w.IsActive && w.FingerprintTemplate == template);

    public async Task<Worker> AddAsync(Worker worker)
    {
        _db.Workers.Add(worker);
        await _db.SaveChangesAsync();
        return worker;
    }

    public async Task UpdateAsync(Worker worker)
    {
        _db.Workers.Update(worker);
        await _db.SaveChangesAsync();
    }
}
