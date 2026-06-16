namespace ShopPOS.WPF.Services.Reports;

public interface ILedgerPdfReportService
{
    void GenerateVendorLedger(VendorLedgerReportData data, string outputPath);
    void GenerateWorkerReport(WorkerEmployeeReportData data, string outputPath);
}
