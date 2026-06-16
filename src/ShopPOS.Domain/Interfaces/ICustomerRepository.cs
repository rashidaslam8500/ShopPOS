using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer> UpsertVisitAsync(string phone);
    Task<IReadOnlyList<Customer>> GetUpdatedSinceAsync(DateTime sinceUtc);
}
