using Microsoft.EntityFrameworkCore;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly PosDbContext _db;

    public ProductRepository(PosDbContext db) => _db = db;

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

    public async Task<Product?> GetByIdAsync(int id) =>
        await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

    public async Task<Product?> GetByBarcodeAsync(string barcode) =>
        await _db.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);

    public async Task<Product?> GetBySkuAsync(string sku) =>
        await _db.Products.FirstOrDefaultAsync(p => p.Sku == sku && p.IsActive);

    public async Task<IReadOnlyList<Product>> SearchAsync(string? search, string? category)
    {
        var query = _db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                (p.Sku != null && p.Sku.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync() =>
        await _db.Products.Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<Product> AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        var tracked = _db.Products.Local.FirstOrDefault(p => p.Id == product.Id)
            ?? await _db.Products.FindAsync(product.Id);

        if (tracked is null)
            throw new InvalidOperationException($"Product {product.Id} not found.");

        if (!ReferenceEquals(tracked, product))
        {
            tracked.Name = product.Name;
            tracked.Category = product.Category;
            tracked.Description = product.Description;
            tracked.Price = product.Price;
            tracked.Stock = product.Stock;
            tracked.Barcode = product.Barcode;
            tracked.Sku = product.Sku;
            tracked.UpdatedAt = product.UpdatedAt;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return;
        product.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateStockAsync(int productId, int quantityChange)
    {
        AdjustStock(productId, quantityChange);
        await _db.SaveChangesAsync();
    }

    public Task AdjustStockAsync(int productId, int quantityChange)
    {
        AdjustStock(productId, quantityChange);
        return Task.CompletedTask;
    }

    private void AdjustStock(int productId, int quantityChange)
    {
        var product = _db.Products.Local.FirstOrDefault(p => p.Id == productId)
            ?? _db.Products.Find(productId)
            ?? throw new InvalidOperationException($"Product {productId} not found.");

        product.Stock += quantityChange;
        if (product.Stock < 0)
            throw new InvalidOperationException($"Insufficient stock for {product.Name}.");
    }

    public Task<bool> BarcodeExistsAsync(string barcode, int? excludeProductId = null)
    {
        var query = _db.Products.Where(p => p.Barcode == barcode && p.IsActive);
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);
        return query.AnyAsync();
    }

    public async Task<int> GetNextInternalBarcodeSequenceAsync()
    {
        var prefix = "KM";
        var existing = await _db.Products
            .Where(p => p.Barcode != null && p.Barcode.StartsWith(prefix))
            .Select(p => p.Barcode!)
            .ToListAsync();

        var max = 0;
        foreach (var code in existing)
        {
            if (code.Length > prefix.Length && int.TryParse(code[prefix.Length..], out var n))
                max = Math.Max(max, n);
        }

        return max + 1;
    }
}
