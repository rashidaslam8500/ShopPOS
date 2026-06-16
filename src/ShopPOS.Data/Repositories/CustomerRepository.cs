using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Models;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly PosDbContext _db;
    public CustomerRepository(PosDbContext db) => _db = db;

    public Task<Customer?> GetByPhoneAsync(string phone) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

    public Task<IReadOnlyList<Customer>> GetAllAsync() =>
        _db.Customers.OrderByDescending(c => c.LastVisit).ToListAsync().ContinueWith(t => (IReadOnlyList<Customer>)t.Result);

    public async Task<Customer> UpsertVisitAsync(string phone)
    {
        var normalized = PhoneHelper.Normalize(phone);
        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == normalized);
        if (existing is not null)
        {
            existing.LastVisit = DateTime.Now;
            existing.VisitCount++;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing;
        }

        var customer = new Customer
        {
            Phone = normalized,
            FirstVisit = DateTime.Now,
            LastVisit = DateTime.Now,
            VisitCount = 1,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public Task<IReadOnlyList<Customer>> GetUpdatedSinceAsync(DateTime sinceUtc) =>
        _db.Customers.Where(c => c.UpdatedAt >= sinceUtc).ToListAsync().ContinueWith(t => (IReadOnlyList<Customer>)t.Result);

    public static string NormalizePhone(string phone) => PhoneHelper.Normalize(phone);
}
