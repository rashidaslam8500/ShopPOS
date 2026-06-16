using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;

namespace ShopPOS.Domain.Interfaces;

public interface IWorkerProfileRepository
{
    Task<Worker?> GetWorkerAsync(int workerId);
    Task<IReadOnlyList<WorkerDailyCash>> GetDailyCashAsync(int workerId, int year, int month);
    Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(int workerId, int year, int month, bool unsettledOnly = false);
    Task<IReadOnlyList<WorkerLeave>> GetLeavesAsync(int workerId, int year, int month);
    Task AddDailyCashAsync(WorkerDailyCash entry);
    Task AddAdvanceAsync(WorkerAdvance entry);
    Task AddLeaveAsync(WorkerLeave entry);
    Task SettleAdvancesAsync(int workerId, int year, int month);
    Task<decimal> SumDailyCashAsync(int workerId, int year, int month);
    Task<decimal> SumUnsettledAdvancesAsync(int workerId, int year, int month);
}

public interface IOwnerPersonalExpenseRepository
{
    Task<IReadOnlyList<OwnerPersonalExpense>> SearchAsync(string? query, DateTime? from, DateTime? to);
    Task AddAsync(OwnerPersonalExpense expense);
    Task<decimal> GetTotalSinceAsync(DateTime since);
}

public interface IVendorRepository
{
    Task<IReadOnlyList<Vendor>> GetAllAsync(bool includeInactive = false);
    Task<Vendor?> GetByIdAsync(int id);
    Task<Vendor> AddAsync(Vendor vendor);
    Task UpdateAsync(Vendor vendor);
    Task<IReadOnlyList<VendorKhataEntry>> GetKhataEntriesAsync(int vendorId, bool includeDeleted = false);
    Task<IReadOnlyList<VendorKhataEntry>> GetDeletedKhataEntriesAsync(int vendorId);
    Task<VendorKhataEntry?> GetKhataEntryByIdAsync(int entryId);
    Task AddKhataEntryAsync(VendorKhataEntry entry);
    Task<int> AddKhataEntryReturnIdAsync(VendorKhataEntry entry);
    Task UpdateKhataEntryAsync(VendorKhataEntry entry);
    Task SoftDeleteKhataEntryAsync(int entryId);
    Task RestoreKhataEntryAsync(int entryId);
    Task PermanentDeleteKhataEntriesAsync(IEnumerable<int> entryIds);
    Task SetAttachmentPathAsync(int entryId, string? attachmentPath);
}
