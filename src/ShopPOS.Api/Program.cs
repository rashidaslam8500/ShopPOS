using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShopPOS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CloudDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CloudDb") ?? "Data Source=cloud-pos.db"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var apiKey = builder.Configuration["ApiKey"] ?? string.Empty;

app.MapPost("/sync", async (HttpContext http, CloudDbContext db) =>
{
    if (!ValidateApiKey(http, apiKey))
        return Results.Unauthorized();

    var payload = await http.Request.ReadFromJsonAsync<SyncPayload>();
    if (payload is null || string.IsNullOrWhiteSpace(payload.ShopId))
        return Results.BadRequest("Invalid sync payload.");

    var syncedAt = payload.SyncedAtUtc == default ? DateTime.UtcNow : payload.SyncedAtUtc;

    await using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        foreach (var sale in payload.Sales)
        {
            var existing = await db.Sales.FirstOrDefaultAsync(s =>
                s.ShopId == payload.ShopId && s.LocalSaleId == sale.Id);

            if (existing is null)
            {
                db.Sales.Add(new CloudSale
                {
                    ShopId = payload.ShopId,
                    LocalSaleId = sale.Id,
                    ReceiptNo = sale.ReceiptNo,
                    SaleDate = sale.SaleDate,
                    Subtotal = sale.Subtotal,
                    DiscountAmount = sale.DiscountAmount,
                    TaxAmount = sale.TaxAmount,
                    Total = sale.Total,
                    NetTotal = sale.NetTotal,
                    PaymentMethod = SyncText(sale.PaymentMethod),
                    CustomerPhone = sale.CustomerPhone,
                    CustomerEmail = sale.CustomerEmail,
                    Status = SyncText(sale.Status),
                    ItemsJson = JsonSerializer.Serialize(sale.Items),
                    SyncedAtUtc = syncedAt
                });
            }
            else
            {
                existing.ReceiptNo = sale.ReceiptNo;
                existing.SaleDate = sale.SaleDate;
                existing.Subtotal = sale.Subtotal;
                existing.DiscountAmount = sale.DiscountAmount;
                existing.TaxAmount = sale.TaxAmount;
                existing.Total = sale.Total;
                existing.NetTotal = sale.NetTotal;
                existing.PaymentMethod = SyncText(sale.PaymentMethod);
                existing.CustomerPhone = sale.CustomerPhone;
                existing.CustomerEmail = sale.CustomerEmail;
                existing.Status = SyncText(sale.Status);
                existing.ItemsJson = JsonSerializer.Serialize(sale.Items);
                existing.SyncedAtUtc = syncedAt;
            }
        }

        foreach (var product in payload.Inventory)
        {
            var existing = await db.Products.FirstOrDefaultAsync(p =>
                p.ShopId == payload.ShopId && p.LocalProductId == product.Id);

            if (existing is null)
            {
                db.Products.Add(new CloudProduct
                {
                    ShopId = payload.ShopId,
                    LocalProductId = product.Id,
                    Name = product.Name,
                    Category = product.Category,
                    Price = product.Price,
                    Stock = product.Stock,
                    Barcode = product.Barcode,
                    Description = product.Description,
                    UpdatedAt = product.UpdatedAt,
                    SyncedAtUtc = syncedAt
                });
            }
            else
            {
                existing.Name = product.Name;
                existing.Category = product.Category;
                existing.Price = product.Price;
                existing.Stock = product.Stock;
                existing.Barcode = product.Barcode;
                existing.Description = product.Description;
                existing.UpdatedAt = product.UpdatedAt;
                existing.SyncedAtUtc = syncedAt;
            }
        }

        foreach (var customer in payload.Customers)
        {
            var existing = await db.Customers.FirstOrDefaultAsync(c =>
                c.ShopId == payload.ShopId && c.LocalCustomerId == customer.Id);

            if (existing is null)
            {
                db.Customers.Add(new CloudCustomer
                {
                    ShopId = payload.ShopId,
                    LocalCustomerId = customer.Id,
                    Phone = customer.Phone,
                    Name = customer.Name,
                    FirstVisit = customer.FirstVisit,
                    LastVisit = customer.LastVisit,
                    VisitCount = customer.VisitCount,
                    SyncedAtUtc = syncedAt
                });
            }
            else
            {
                existing.Phone = customer.Phone;
                existing.Name = customer.Name;
                existing.FirstVisit = customer.FirstVisit;
                existing.LastVisit = customer.LastVisit;
                existing.VisitCount = customer.VisitCount;
                existing.SyncedAtUtc = syncedAt;
            }
        }

        db.SyncLogs.Add(new CloudSyncLog
        {
            ShopId = payload.ShopId,
            SyncedAtUtc = syncedAt,
            SalesCount = payload.Sales.Count,
            ProductsCount = payload.Inventory.Count,
            CustomersCount = payload.Customers.Count
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }

    return Results.Ok(new
    {
        message = "Sync accepted",
        sales = payload.Sales.Count,
        products = payload.Inventory.Count,
        customers = payload.Customers.Count
    });
});

app.MapGet("/api/dashboard/{shopId}", async (string shopId, HttpContext http, CloudDbContext db) =>
{
    if (!ValidateApiKey(http, apiKey))
        return Results.Unauthorized();

    var today = DateTime.Today;
    var salesToday = await db.Sales
        .Where(s => s.ShopId == shopId && s.SaleDate >= today && s.SaleDate < today.AddDays(1))
        .ToListAsync();

    var totalProducts = await db.Products.CountAsync(p => p.ShopId == shopId);
    var lowStock = await db.Products.CountAsync(p => p.ShopId == shopId && p.Stock <= 5);
    var totalCustomers = await db.Customers.CountAsync(c => c.ShopId == shopId);
    var lastSync = await db.SyncLogs
        .Where(l => l.ShopId == shopId)
        .OrderByDescending(l => l.SyncedAtUtc)
        .Select(l => l.SyncedAtUtc)
        .FirstOrDefaultAsync();

    return Results.Ok(new
    {
        shopId,
        lastSyncUtc = lastSync,
        todaySalesCount = salesToday.Count,
        todayRevenue = salesToday.Sum(s => s.NetTotal),
        totalProducts,
        lowStock,
        totalCustomers
    });
});

app.MapGet("/api/sales/{shopId}", async (string shopId, HttpContext http, CloudDbContext db, int take = 50) =>
{
    if (!ValidateApiKey(http, apiKey))
        return Results.Unauthorized();

    var sales = await db.Sales
        .Where(s => s.ShopId == shopId)
        .OrderByDescending(s => s.SaleDate)
        .Take(Math.Clamp(take, 1, 200))
        .Select(s => new
        {
            s.ReceiptNo,
            s.SaleDate,
            s.NetTotal,
            s.PaymentMethod,
            s.CustomerPhone,
            s.CustomerEmail
        })
        .ToListAsync();

    return Results.Ok(sales);
});

app.MapGet("/api/products/{shopId}", async (string shopId, HttpContext http, CloudDbContext db) =>
{
    if (!ValidateApiKey(http, apiKey))
        return Results.Unauthorized();

    var products = await db.Products
        .Where(p => p.ShopId == shopId)
        .OrderBy(p => p.Name)
        .Select(p => new { p.Name, p.Category, p.Price, p.Stock, p.Barcode })
        .ToListAsync();

    return Results.Ok(products);
});

app.Run("http://localhost:5050");

static bool ValidateApiKey(HttpContext http, string expectedKey)
{
    if (string.IsNullOrWhiteSpace(expectedKey))
        return false;

    if (!http.Request.Headers.TryGetValue("X-Api-Key", out var provided))
        return false;

    return string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal);
}

static string SyncText(object? value) => value?.ToString() ?? string.Empty;

record SyncPayload(
    string ShopId,
    DateTime SyncedAtUtc,
    List<SyncSale> Sales,
    List<SyncProduct> Inventory,
    List<SyncCustomer> Customers);

record SyncSale(
    int Id,
    string ReceiptNo,
    DateTime SaleDate,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal Total,
    decimal NetTotal,
    object PaymentMethod,
    string? CustomerPhone,
    string? CustomerEmail,
    object Status,
    List<SyncSaleItem> Items);

record SyncSaleItem(int ProductId, string ProductName, int Quantity, decimal UnitPriceAtSale, decimal LineTotal);

record SyncProduct(
    int Id,
    string Name,
    string Category,
    decimal Price,
    int Stock,
    string? Barcode,
    string? Description,
    DateTime UpdatedAt);

record SyncCustomer(
    int Id,
    string Phone,
    string? Name,
    DateTime FirstVisit,
    DateTime LastVisit,
    int VisitCount);
