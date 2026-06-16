using QuestPDF.Infrastructure;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShopPOS.Business;
using ShopPOS.Business.Services;
using ShopPOS.Data;
using ShopPOS.Data.Security;
using ShopPOS.Domain.Models;
using ShopPOS.WPF.Assets;
using ShopPOS.WPF.Services;
using ShopPOS.WPF.Services.Reports;
using ShopPOS.WPF.ViewModels;
using ShopPOS.WPF.Windows;

namespace ShopPOS.WPF;

public partial class App : Application
{
    private readonly IHost _host;

    public static IServiceProvider Services => ((App)Current)._host.Services;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Connection string 'DefaultConnection' not found in appsettings.json. " +
                        "Configure SQL Server connection for BhaiGeePOS database.");

                var hardware = context.Configuration
                    .GetSection(PosHardwareOptions.SectionName)
                    .Get<PosHardwareOptions>() ?? new PosHardwareOptions();
                var cloudSync = context.Configuration
                    .GetSection(CloudSyncOptions.SectionName)
                    .Get<CloudSyncOptions>() ?? new CloudSyncOptions();
                var sms = context.Configuration
                    .GetSection(SmsOptions.SectionName)
                    .Get<SmsOptions>() ?? new SmsOptions();
                var whatsApp = context.Configuration
                    .GetSection(WhatsAppOptions.SectionName)
                    .Get<WhatsAppOptions>() ?? new WhatsAppOptions();
                var email = context.Configuration
                    .GetSection(EmailOptions.SectionName)
                    .Get<EmailOptions>() ?? new EmailOptions();
                UnprotectStartupSecrets(cloudSync, sms, whatsApp, email);

                services.AddSingleton(hardware);
                services.AddSingleton(sms);
                services.AddSingleton(whatsApp);
                services.AddSingleton(email);

                services.AddDataLayer(connectionString);
                services.AddBusinessLayer();
                services.AddCloudSyncWorker(cloudSync);
                services.AddSingleton<IReceiptLogoProvider, WpfReceiptLogoProvider>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<IAppShell>(sp => sp.GetRequiredService<MainViewModel>());
                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<ForgotPasswordViewModel>();
                services.AddTransient<SecurityQuestionsSetupViewModel>();
                services.AddTransient<ForgotPasswordWindow>();
                services.AddTransient<SecurityQuestionsSetupWindow>();
                services.AddTransient<CheckoutViewModel>();
                services.AddTransient<ProductsViewModel>();
                services.AddTransient<SalesViewModel>();
                services.AddTransient<ReturnsViewModel>();
                services.AddTransient<TrashViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<AuditLogsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<StaffExpensesViewModel>();
                services.AddTransient<WorkerProfileViewModel>();
                services.AddTransient<WorkerProfileWindow>();
                services.AddTransient<OwnerExpensesViewModel>();
                services.AddTransient<OwnerExpensesWindow>();
                services.AddSingleton<IVendorBillStorageService, VendorBillStorageService>();
                services.AddSingleton<ILedgerPdfReportService, QuestPdfLedgerReportService>();
                services.AddTransient<VendorBillViewerWindow>();
                services.AddTransient<VendorPurchaseEntryWindow>();
                services.AddTransient<VendorProfileUpdateWindow>();
                services.AddTransient<VendorKhataViewModel>();
                services.AddTransient<FingerprintEnrollDialog>();
                services.AddTransient<LoginWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred.\n\n{args.Exception.Message}",
                "Bhai Gee POS — Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        await InitializeAppDataAsync();

        await _host.StartAsync();

        await EnsureCloudServicesAsync();

        var initTask = Task.CompletedTask;

        var splash = new SplashScreenWindow();
        splash.Show();
        await splash.WaitForCompletionAsync();

        try
        {
            await initTask;
        }
        catch
        {
            return;
        }

        try
        {
            using (var loginScope = _host.Services.CreateScope())
            {
                var login = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
                login.WindowState = WindowState.Maximized;
                Current.MainWindow = login;
                login.Activate();

                if (login.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }

                var auth = loginScope.ServiceProvider.GetRequiredService<IAuthService>();
                while (!await auth.HasSecurityQuestionsAsync())
                {
                    var setup = loginScope.ServiceProvider.GetRequiredService<SecurityQuestionsSetupWindow>();
                    setup.Owner = login;
                    if (setup.ShowDialog() == true && await auth.HasSecurityQuestionsAsync())
                        break;

                    var retry = MessageBox.Show(
                        "Security questions are required before using the POS.\n\nSet them now?",
                        "Bhai Gee POS — Security Setup",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (retry != MessageBoxResult.Yes)
                    {
                        Shutdown();
                        return;
                    }
                }
            }

            var main = _host.Services.GetRequiredService<MainWindow>();
            main.Closed += (_, _) => Shutdown();
            MainWindow = main;
            main.WindowState = WindowState.Maximized;
            main.Show();
            main.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open the main window after login.\n\n{ex.Message}",
                "Bhai Gee POS — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    private async Task EnsureCloudServicesAsync()
    {
        try
        {
            var cloudOptions = _host.Services.GetRequiredService<CloudSyncOptions>();
            await CloudApiLauncher.EnsureRunningAsync(cloudOptions);

            if (!cloudOptions.Enabled)
                return;

            using var scope = _host.Services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ICloudSyncService>();
            await sync.SyncAsync();
        }
        catch
        {
            // Non-fatal: cloud sync can be retried from Settings.
        }
    }

    private async Task InitializeAppDataAsync()
    {
        try
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            await DatabaseSeeder.InitializeWithRetryAsync(db);

            var integration = scope.ServiceProvider.GetRequiredService<IIntegrationSettingsService>();
            await integration.LoadAndApplyAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not connect to SQL Server.\n\n{ex.Message}\n\n" +
                "1. Ensure SQL Server / LocalDB is running\n" +
                "2. Create database 'BhaiGeePOS' in SSMS (or let the app create it via EnsureCreated)\n" +
                "3. Update ConnectionStrings:DefaultConnection in appsettings.json\n" +
                "4. If SQL Server was starting up, try launching the app again",
                "Bhai Gee POS — Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            throw;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private static void UnprotectStartupSecrets(
        CloudSyncOptions cloudSync,
        SmsOptions sms,
        WhatsAppOptions whatsApp,
        EmailOptions email)
    {
        if (SettingsSecretProtector.IsProtected(cloudSync.ApiKey))
            cloudSync.ApiKey = SettingsSecretProtector.Unprotect(cloudSync.ApiKey);

        if (SettingsSecretProtector.IsProtected(sms.ApiKey))
            sms.ApiKey = SettingsSecretProtector.Unprotect(sms.ApiKey);

        if (SettingsSecretProtector.IsProtected(sms.ApiUsername))
            sms.ApiUsername = SettingsSecretProtector.Unprotect(sms.ApiUsername);

        if (SettingsSecretProtector.IsProtected(whatsApp.ApiKey))
            whatsApp.ApiKey = SettingsSecretProtector.Unprotect(whatsApp.ApiKey);

        if (SettingsSecretProtector.IsProtected(email.Password))
            email.Password = SettingsSecretProtector.Unprotect(email.Password);
    }
}
