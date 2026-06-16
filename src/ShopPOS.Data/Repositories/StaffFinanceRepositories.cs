using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class WorkerProfileRepository : IWorkerProfileRepository
{
    private readonly PosDbContext _db;
    public WorkerProfileRepository(PosDbContext db) => _db = db;

    public Task<Worker?> GetWorkerAsync(int workerId) =>
        _db.Workers.FirstOrDefaultAsync(w => w.Id == workerId && w.IsActive);

    public Task<IReadOnlyList<WorkerDailyCash>> GetDailyCashAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return _db.WorkerDailyCash
            .Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end)
            .OrderByDescending(x => x.Date)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<WorkerDailyCash>)t.Result);
    }

    public Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(int workerId, int year, int month, bool unsettledOnly = false)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var query = _db.WorkerAdvances.Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end);
        if (unsettledOnly)
            query = query.Where(x => !x.IsSettled);
        return query.OrderByDescending(x => x.Date).ToListAsync()
            .ContinueWith(t => (IReadOnlyList<WorkerAdvance>)t.Result);
    }

    public Task<IReadOnlyList<WorkerLeave>> GetLeavesAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return _db.WorkerLeaves
            .Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end)
            .OrderByDescending(x => x.Date)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<WorkerLeave>)t.Result);
    }

    public async Task AddDailyCashAsync(WorkerDailyCash entry)
    {
        _db.WorkerDailyCash.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task AddAdvanceAsync(WorkerAdvance entry)
    {
        _db.WorkerAdvances.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task AddLeaveAsync(WorkerLeave entry)
    {
        _db.WorkerLeaves.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task SettleAdvancesAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var advances = await _db.WorkerAdvances
            .Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end && !x.IsSettled)
            .ToListAsync();
        foreach (var a in advances)
            a.IsSettled = true;
        await _db.SaveChangesAsync();
    }

    public Task<decimal> SumDailyCashAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return _db.WorkerDailyCash
            .Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end)
            .SumAsync(x => x.Amount);
    }

    public Task<decimal> SumUnsettledAdvancesAsync(int workerId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return _db.WorkerAdvances
            .Where(x => x.WorkerId == workerId && x.Date >= start && x.Date < end && !x.IsSettled)
            .SumAsync(x => x.Amount);
    }
}

public class OwnerPersonalExpenseRepository : IOwnerPersonalExpenseRepository
{
    private readonly PosDbContext _db;
    public OwnerPersonalExpenseRepository(PosDbContext db) => _db = db;

    public async Task<IReadOnlyList<OwnerPersonalExpense>> SearchAsync(string? query, DateTime? from, DateTime? to)
    {
        var q = _db.OwnerPersonalExpenses.AsQueryable();
        if (from.HasValue)
            q = q.Where(x => x.Date >= from.Value.Date);
        if (to.HasValue)
            q = q.Where(x => x.Date <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(x => x.Description.Contains(term) || x.Category.ToString().Contains(term));
        }
        return await q.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToListAsync();
    }

    public async Task AddAsync(OwnerPersonalExpense expense)
    {
        _db.OwnerPersonalExpenses.Add(expense);
        await _db.SaveChangesAsync();
    }

    public Task<decimal> GetTotalSinceAsync(DateTime since) =>
        _db.OwnerPersonalExpenses.Where(x => x.Date >= since).SumAsync(x => x.Amount);
}

public class VendorRepository : IVendorRepository
{
    private readonly PosDbContext _db;
    public VendorRepository(PosDbContext db) => _db = db;

    public Task<IReadOnlyList<Vendor>> GetAllAsync(bool includeInactive = false)
    {
        var q = _db.Vendors.AsQueryable();
        if (!includeInactive)
            q = q.Where(v => v.IsActive);
        return q.OrderBy(v => v.Name).ToListAsync()
            .ContinueWith(t => (IReadOnlyList<Vendor>)t.Result);
    }

    public Task<Vendor?> GetByIdAsync(int id) =>
        _db.Vendors.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Vendor> AddAsync(Vendor vendor)
    {
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        return vendor;
    }

    public async Task UpdateAsync(Vendor vendor)
    {
        _db.Vendors.Update(vendor);
        await _db.SaveChangesAsync();
    }

    public Task<IReadOnlyList<VendorKhataEntry>> GetKhataEntriesAsync(int vendorId, bool includeDeleted = false)
    {
        var q = _db.VendorKhataEntries.Where(x => x.VendorId == vendorId);
        if (!includeDeleted)
            q = q.Where(x => !x.IsDeleted);
        return q.OrderBy(x => x.Date).ThenBy(x => x.Id)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<VendorKhataEntry>)t.Result);
    }

    public Task<IReadOnlyList<VendorKhataEntry>> GetDeletedKhataEntriesAsync(int vendorId) =>
        _db.VendorKhataEntries
            .Where(x => x.VendorId == vendorId && x.IsDeleted)
            .OrderByDescending(x => x.DeletedAt)
            .ToListAsync()
            .ContinueWith(t => (IReadOnlyList<VendorKhataEntry>)t.Result);

    public Task<VendorKhataEntry?> GetKhataEntryByIdAsync(int entryId) =>
        _db.VendorKhataEntries.FirstOrDefaultAsync(x => x.Id == entryId);

    public async Task AddKhataEntryAsync(VendorKhataEntry entry)
    {
        _db.VendorKhataEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<int> AddKhataEntryReturnIdAsync(VendorKhataEntry entry)
    {
        _db.VendorKhataEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    public async Task UpdateKhataEntryAsync(VendorKhataEntry entry)
    {
        _db.VendorKhataEntries.Update(entry);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteKhataEntryAsync(int entryId)
    {
        var entry = await _db.VendorKhataEntries.FindAsync(entryId)
            ?? throw new InvalidOperationException("Khata entry not found.");
        if (entry.IsDeleted)
            return;
        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RestoreKhataEntryAsync(int entryId)
    {
        var entry = await _db.VendorKhataEntries.FindAsync(entryId)
            ?? throw new InvalidOperationException("Khata entry not found.");
        if (!entry.IsDeleted)
            throw new InvalidOperationException("Only trashed khata entries can be restored.");
        entry.IsDeleted = false;
        entry.DeletedAt = null;
        await _db.SaveChangesAsync();
    }

    public async Task PermanentDeleteKhataEntriesAsync(IEnumerable<int> entryIds)
    {
        var ids = entryIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var entries = await _db.VendorKhataEntries
            .Where(e => ids.Contains(e.Id) && e.IsDeleted)
            .ToListAsync();
        if (entries.Count == 0)
            throw new InvalidOperationException("No trashed entries were found for permanent deletion.");

        _db.VendorKhataEntries.RemoveRange(entries);
        await _db.SaveChangesAsync();
    }

    public async Task SetAttachmentPathAsync(int entryId, string? attachmentPath)
    {
        var entry = await _db.VendorKhataEntries.FindAsync(entryId)
            ?? throw new InvalidOperationException("Khata entry not found.");
        entry.AttachmentPath = attachmentPath;
        await _db.SaveChangesAsync();
    }
}
