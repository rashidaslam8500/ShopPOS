using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.ViewModels;

public partial class MainViewModel : ViewModelBase, IAppShell
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CurrentSession _session;

    [ObservableProperty] private TabPageViewModel? _selectedTab;
    [ObservableProperty] private string _shopName = "KitchenMart.pk";
    [ObservableProperty] private string _userDisplay = string.Empty;
    [ObservableProperty] private string _roleLabel = string.Empty;
    [ObservableProperty] private string _clockText = DateTime.Now.ToString("g");
    [ObservableProperty] private bool _isOwner;
    [ObservableProperty] private bool _hasOpenTabs;

    public ObservableCollection<TabPageViewModel> Tabs { get; } = new();

    public MainViewModel(
        IServiceScopeFactory scopeFactory,
        CurrentSession session)
    {
        _scopeFactory = scopeFactory;
        _session = session;
        Tabs.CollectionChanged += OnTabsCollectionChanged;
        _ = InitAsync();

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => ClockText = DateTime.Now.ToString("g");
        timer.Start();
    }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasOpenTabs = Tabs.Count > 0;
        if (e.OldItems is not null)
        {
            foreach (TabPageViewModel tab in e.OldItems)
                tab.Dispose();
        }
    }

    private async Task InitAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var config = await settings.GetConfigAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ShopName = config.ShopName;
                UserDisplay = _session.User?.DisplayName ?? "";
                RoleLabel = _session.IsOwner ? "Owner" : "Salesman";
                IsOwner = _session.IsOwner;
            });

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => OpenTab(TabFeature.Billing));
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                System.Windows.MessageBox.Show(
                    $"Could not load the dashboard.\n\n{ex.Message}",
                    "Bhai Gee POS — Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error));
        }
    }

    public void OpenTab(TabFeature feature)
    {
        if (feature.RequiresOwner() && !IsOwner)
            return;

        var existing = Tabs.FirstOrDefault(t => t.Feature == feature);
        if (existing is not null)
        {
            SelectedTab = existing;
            if (existing.Content is SettingsViewModel settingsVm)
                _ = settingsVm.RefreshAsync();
            return;
        }

        var scope = _scopeFactory.CreateScope();
        try
        {
            var content = ResolveTabContent(scope.ServiceProvider, feature);
            var title = feature.GetTitle();

            var tab = new TabPageViewModel(title, feature, content, scope, CloseTab);
            Tabs.Add(tab);
            SelectedTab = tab;
        }
        catch (Exception ex)
        {
            scope.Dispose();
            System.Windows.MessageBox.Show(
                $"Could not open {feature.GetTitle()}.\n\n{ex.Message}",
                "Bhai Gee POS — Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private static ViewModelBase ResolveTabContent(IServiceProvider services, TabFeature feature) =>
        feature switch
        {
            TabFeature.Billing => services.GetRequiredService<CheckoutViewModel>(),
            TabFeature.Returns => services.GetRequiredService<ReturnsViewModel>(),
            TabFeature.Products => services.GetRequiredService<ProductsViewModel>(),
            TabFeature.Sales => services.GetRequiredService<SalesViewModel>(),
            TabFeature.Dashboard => services.GetRequiredService<DashboardViewModel>(),
            TabFeature.AuditLogs => services.GetRequiredService<AuditLogsViewModel>(),
            TabFeature.Settings => services.GetRequiredService<SettingsViewModel>(),
            TabFeature.Trash => services.GetRequiredService<TrashViewModel>(),
            TabFeature.StaffExpenses => services.GetRequiredService<StaffExpensesViewModel>(),
            TabFeature.VendorKhata => services.GetRequiredService<VendorKhataViewModel>(),
            TabFeature.OwnerExpenses => services.GetRequiredService<OwnerExpensesViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
        };

    private void CloseTab(TabPageViewModel tab)
    {
        var index = Tabs.IndexOf(tab);
        if (index < 0)
            return;

        var wasSelected = ReferenceEquals(SelectedTab, tab);
        Tabs.RemoveAt(index);

        if (!wasSelected)
            return;

        if (Tabs.Count == 0)
        {
            SelectedTab = null;
            return;
        }

        SelectedTab = index < Tabs.Count ? Tabs[index] : Tabs[^1];
    }

    [RelayCommand] private void NavigateCheckout() => OpenTab(TabFeature.Billing);
    [RelayCommand] private void NavigateProducts() => OpenTab(TabFeature.Products);
    [RelayCommand] private void NavigateSales() => OpenTab(TabFeature.Sales);
    [RelayCommand] private void NavigateReturns() => OpenTab(TabFeature.Returns);
    [RelayCommand] private void NavigateDashboard() => OpenTab(TabFeature.Dashboard);
    [RelayCommand] private void NavigateAudit() => OpenTab(TabFeature.AuditLogs);
    [RelayCommand] private void NavigateSettings() => OpenTab(TabFeature.Settings);
    [RelayCommand] private void NavigateTrash() => OpenTab(TabFeature.Trash);
    [RelayCommand] private void NavigateStaffExpenses() => OpenTab(TabFeature.StaffExpenses);
    [RelayCommand] private void NavigateVendorKhata() => OpenTab(TabFeature.VendorKhata);
    [RelayCommand] private void OpenOwnerExpenses() => OpenTab(TabFeature.OwnerExpenses);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        foreach (var tab in Tabs.ToList())
            tab.Dispose();
        Tabs.Clear();
        SelectedTab = null;

        using var scope = _scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAuthService>().LogoutAsync();
        System.Windows.Application.Current.Shutdown();
    }

    public void RefreshShopName(string name) => ShopName = name;

    public async Task OpenReturnsForInvoiceAsync(string scannedValue)
    {
        var receiptNo = InvoiceScanHelper.Normalize(scannedValue);
        if (string.IsNullOrWhiteSpace(receiptNo))
            return;

        OpenTab(TabFeature.Returns);

        if (SelectedTab?.Content is ReturnsViewModel returnsVm)
            await returnsVm.LoadFromScannerAsync(receiptNo);
    }
}
