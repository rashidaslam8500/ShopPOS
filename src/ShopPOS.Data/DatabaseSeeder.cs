using Microsoft.EntityFrameworkCore;
using ShopPOS.Data.Security;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;

namespace ShopPOS.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PosDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSchemaUpdater.ApplyAsync(db);

        if (!await db.Users.AnyAsync())
        {
            var (ownerHash, ownerSalt) = PasswordHasher.HashPassword("owner123");
            var (salesHash, salesSalt) = PasswordHasher.HashPassword("sales123");

            db.Users.AddRange(
                new User { Username = "owner", PasswordHash = ownerHash, PasswordSalt = ownerSalt, Role = UserRole.Owner, DisplayName = "Shop Owner" },
                new User { Username = "sales", PasswordHash = salesHash, PasswordSalt = salesSalt, Role = UserRole.Salesman, DisplayName = "Sales Counter" }
            );
        }

        if (!await db.ShopSettings.AnyAsync())
        {
            db.ShopSettings.AddRange(
                new ShopSetting { Key = "ShopName", Value = "KitchenMart.pk" },
                new ShopSetting { Key = "Address", Value = "Karkhana Bazar, Sargodha" },
                new ShopSetting { Key = "Phone", Value = "03006027106" },
                new ShopSetting { Key = "CurrencySymbol", Value = "Rs." },
                new ShopSetting { Key = "TaxRate", Value = "0" },
                new ShopSetting { Key = "ReceiptFooter", Value = "Thank you for shopping at KitchenMart.pk!" }
            );
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Royal Dinner Set (24pc)", Category = "Dinner Sets", Price = 8500, Stock = 18, Barcode = "BG2001", Sku = "DS-024" },
                new Product { Name = "Classic Dinner Set (6pc)", Category = "Dinner Sets", Price = 2500, Stock = 40, Barcode = "BG2002", Sku = "DS-006" },
                new Product { Name = "Premium Tea Set (12pc)", Category = "Tea Sets", Price = 3200, Stock = 28, Barcode = "BG2003", Sku = "TS-012" },
                new Product { Name = "Tea Cup & Saucer (6pc)", Category = "Tea Sets", Price = 1800, Stock = 35, Barcode = "BG2004", Sku = "TS-006" },
                new Product { Name = "Crystal Glassware Set (6pc)", Category = "Glassware", Price = 2400, Stock = 30, Barcode = "BG2005", Sku = "GW-006" },
                new Product { Name = "Glass Tumbler Set (6pc)", Category = "Glassware", Price = 1200, Stock = 50, Barcode = "BG2006", Sku = "GW-TMB" },
                new Product { Name = "Non-Stick Cooking Pot 3L", Category = "Non-Stick Pots", Price = 3200, Stock = 20, Barcode = "BG2007", Sku = "NS-3L" },
                new Product { Name = "Non-Stick Fry Pan 28cm", Category = "Non-Stick Pots", Price = 2100, Stock = 32, Barcode = "BG2008", Sku = "NS-FRY" },
                new Product { Name = "Melamine Dinner Plate (Loose)", Category = "Loose Melamine", Price = 350, Stock = 120, Barcode = "BG2009", Sku = "LM-PLT" },
                new Product { Name = "Melamine Serving Bowl (Loose)", Category = "Loose Melamine", Price = 480, Stock = 85, Barcode = "BG2010", Sku = "LM-BWL" },
                new Product { Name = "Melamine Tray Large (Loose)", Category = "Loose Melamine", Price = 650, Stock = 60, Barcode = "BG2011", Sku = "LM-TRY" },
                new Product { Name = "Stainless Steel Thali Set", Category = "Dinner Sets", Price = 950, Stock = 45, Barcode = "BG2012", Sku = "DS-STL" }
            );
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task InitializeWithRetryAsync(
        PosDbContext db,
        int maxAttempts = 5,
        int delayMilliseconds = 2000,
        CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await SeedAsync(db, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientDatabaseError(ex))
            {
                lastError = ex;
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
            catch (Exception ex)
            {
                lastError = ex;
                break;
            }
        }

        throw lastError ?? new InvalidOperationException("Database initialization failed.");
    }

    private static bool IsTransientDatabaseError(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;

            var message = current.Message;
            if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || message.Contains("network-related", StringComparison.OrdinalIgnoreCase)
                || message.Contains("establishing a connection", StringComparison.OrdinalIgnoreCase)
                || message.Contains("transient", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
