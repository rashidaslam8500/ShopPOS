namespace ShopPOS.WPF.ViewModels;

public interface IAppShell
{
    void RefreshShopName(string name);
    Task OpenReturnsForInvoiceAsync(string scannedValue);
}
