using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface ISaleRepository
{
    Task<IReadOnlyList<Sale>> GetAllAsync(DateTime? date = null, bool includeDeleted = false);
    Task<IReadOnlyList<Sale>> GetDeletedAsync();
    Task<Sale?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<Sale?> GetByReceiptNoAsync(string receiptNo, bool includeDeleted = false);
    Task<Sale> AddAsync(Sale sale);
    Task UpdateAsync(Sale sale);
    Task<int> GetNextReceiptSequenceAsync();
    Task RunInTransactionAsync(Func<Task> work);
    Task StageReturnAsync(SaleReturn saleReturn);
    Task StageAmendmentAsync(SaleAmendment amendment);
    Task StageItemAsync(SaleItem item);
    Task<IReadOnlyList<Sale>> GetSalesSinceAsync(DateTime sinceUtc);
    Task PermanentDeleteAsync(int saleId);
}
