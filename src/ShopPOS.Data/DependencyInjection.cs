using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopPOS.Data.Repositories;
using ShopPOS.Domain.Interfaces;

namespace ShopPOS.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PosDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IExpenseAndCashRepository, ExpenseAndCashRepository>();
        services.AddScoped<IWorkerProfileRepository, WorkerProfileRepository>();
        services.AddScoped<IOwnerPersonalExpenseRepository, OwnerPersonalExpenseRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
