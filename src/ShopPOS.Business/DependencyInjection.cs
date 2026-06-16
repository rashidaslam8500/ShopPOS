using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        services.AddSingleton<CurrentSession>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IIntegrationSettingsService, IntegrationSettingsService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IExpenseAndCashService, ExpenseAndCashService>();
        services.AddScoped<IWorkerProfileService, WorkerProfileService>();
        services.AddScoped<IOwnerPersonalExpenseService, OwnerPersonalExpenseService>();
        services.AddScoped<IVendorKhataService, VendorKhataService>();
        services.AddSingleton<IFingerprintScannerService, FingerprintScannerService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<IBarcodeService, BarcodeService>();
        services.AddSingleton<IReceiptPrintService, ReceiptPrintService>();

        services.AddScoped<ICustomerNotificationService, CustomerNotificationService>();

        services.AddHttpClient<ISmsService, SmsService>();
        services.AddHttpClient<IWhatsAppService, WhatsAppService>();
        services.AddHttpClient<ICloudSyncService, CloudSyncService>();
        services.AddSingleton<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddCloudSyncWorker(this IServiceCollection services, CloudSyncOptions options)
    {
        services.AddSingleton(options);
        services.AddHostedService<CloudSyncBackgroundWorker>();
        return services;
    }
}
