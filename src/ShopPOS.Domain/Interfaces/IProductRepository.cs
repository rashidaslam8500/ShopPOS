using ShopPOS.Domain.Entities;

namespace ShopPOS.Domain.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetBySkuAsync(string sku);
    Task<IReadOnlyList<Product>> SearchAsync(string? search, string? category);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<Product> AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task UpdateStockAsync(int productId, int quantityChange);
    Task AdjustStockAsync(int productId, int quantityChange);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeProductId = null);
    Task<int> GetNextInternalBarcodeSequenceAsync();
}
