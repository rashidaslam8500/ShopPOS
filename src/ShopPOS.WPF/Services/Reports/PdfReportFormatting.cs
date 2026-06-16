namespace ShopPOS.WPF.Services.Reports;

public static class PdfReportFormatting
{
    public static string FormatAmount(decimal amount)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        return rounded.ToString("N0");
    }
}
