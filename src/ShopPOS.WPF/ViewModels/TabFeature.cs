namespace ShopPOS.WPF.ViewModels;

public enum TabFeature
{
    Billing,
    Returns,
    Products,
    Sales,
    Dashboard,
    AuditLogs,
    Settings,
    Trash,
    StaffExpenses,
    VendorKhata,
    OwnerExpenses
}

public static class TabFeatureExtensions
{
    public static string GetTitle(this TabFeature feature) => feature switch
    {
        TabFeature.Billing => "Billing",
        TabFeature.Returns => "Returns",
        TabFeature.Products => "Inventory",
        TabFeature.Sales => "Sales Report",
        TabFeature.Dashboard => "Dashboard",
        TabFeature.AuditLogs => "Audit Logs",
        TabFeature.Settings => "Settings",
        TabFeature.Trash => "Trash Bin",
        TabFeature.StaffExpenses => "Staff & Expenses",
        TabFeature.VendorKhata => "Vendor Ledger",
        TabFeature.OwnerExpenses => "Owner Expenses",
        _ => feature.ToString()
    };

    public static bool RequiresOwner(this TabFeature feature) => feature switch
    {
        TabFeature.Billing => false,
        TabFeature.Returns => false,
        _ => true
    };
}
