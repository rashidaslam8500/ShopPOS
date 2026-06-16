using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface IWorkerRepository
{
    Task<IReadOnlyList<Worker>> GetAllAsync(bool includeInactive = false);
    Task<Worker?> GetByIdAsync(int id, bool includeInactive = false);
    Task<Worker?> GetByFingerprintTemplateAsync(string template);
    Task<Worker> AddAsync(Worker worker);
    Task UpdateAsync(Worker worker);
}
