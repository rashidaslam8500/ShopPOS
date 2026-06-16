using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Business.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? search = null, string? category = null);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetByLookupCodeAsync(string code);
    Task<Product> SaveProductAsync(Product product, bool isNew);
    Task DeleteProductAsync(int id);
    Task<string> GenerateUniqueBarcodeAsync(int? excludeProductId = null);
    string GenerateBarcode(int productId);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IAuditService _audit;

    public ProductService(IProductRepository products, IAuditService audit)
    {
        _products = products;
        _audit = audit;
    }

    public Task<IReadOnlyList<Product>> GetProductsAsync(string? search = null, string? category = null) =>
        _products.SearchAsync(search, category);

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        var cats = await _products.GetCategoriesAsync();
        return new[] { "All" }.Concat(cats).ToList();
    }

    public Task<Product?> GetByBarcodeAsync(string barcode) =>
        _products.GetByBarcodeAsync(barcode);

    public async Task<Product?> GetByLookupCodeAsync(string code)
    {
        var trimmed = code.Trim();
        var byBarcode = await _products.GetByBarcodeAsync(trimmed);
        if (byBarcode is not null)
            return byBarcode;

        return await _products.GetBySkuAsync(trimmed);
    }

    public async Task<Product> SaveProductAsync(Product product, bool isNew)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentException("Product name is required.");

        if (isNew)
        {
            if (string.IsNullOrWhiteSpace(product.Barcode))
                product.Barcode = $"BG{DateTime.Now:yyMMdd}{new Random().Next(1000, 9999)}";

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            var saved = await _products.AddAsync(product);
            await _audit.LogAsync(AuditActionType.ProductAdded, "Product", saved.Id.ToString(),
                $"Added {saved.Name} @ Rs.{saved.Price:N2}", newValues: saved.Name);
            return saved;
        }

        var existing = await _products.GetByIdAsync(product.Id)
            ?? throw new InvalidOperationException("Product not found.");

        var oldPrice = existing.Price;
        existing.Name = product.Name;
        existing.Category = product.Category;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Stock = product.Stock;
        existing.Barcode = product.Barcode;
        existing.Sku = product.Sku;
        existing.UpdatedAt = DateTime.UtcNow;
        await _products.UpdateAsync(existing);

        var action = oldPrice != product.Price ? AuditActionType.PriceChanged : AuditActionType.ProductUpdated;
        await _audit.LogAsync(action, "Product", existing.Id.ToString(),
            $"Updated {existing.Name}",
            oldValues: $"{{\"price\":{oldPrice}}}",
            newValues: $"{{\"price\":{existing.Price}}}");

        return existing;
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product is null) return;
        await _products.DeleteAsync(id);
        await _audit.LogAsync(AuditActionType.ProductDeleted, "Product", id.ToString(), $"Deleted {product.Name}");
    }

    public async Task<string> GenerateUniqueBarcodeAsync(int? excludeProductId = null)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var seq = await _products.GetNextInternalBarcodeSequenceAsync() + attempt;
            var code = $"KM{seq:D6}";
            if (!await _products.BarcodeExistsAsync(code, excludeProductId))
                return code;
        }

        return $"KM{DateTime.Now:yyMMddHHmmss}";
    }

    public string GenerateBarcode(int productId) => $"BG{productId:D6}";
}
