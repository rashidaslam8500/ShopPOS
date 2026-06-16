using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Text;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IReceiptPrintService
{
    void PrintReceipt(Sale sale, ShopConfig config);
    string BuildReceiptText(Sale sale, ShopConfig config);
}

/// <summary>
/// Thermal receipt printing (80mm) with B&amp;W logo, scannable Code128 barcode, and ESC/POS cash drawer pulse.
/// </summary>
public class ReceiptPrintService : IReceiptPrintService
{
    private const float BarcodeHeight = 50f;
    private const float BarcodeReservedHeight = 72f;

    private readonly IReceiptLogoProvider? _logoProvider;
    private readonly IBarcodeService _barcodes;
    private readonly PosHardwareOptions _hardware;
    private Sale? _currentSale;
    private ShopConfig? _currentConfig;
    private List<string> _printLines = [];
    private int _lineIndex;
    private bool _bodyComplete;
    private bool _barcodeDrawn;

    public ReceiptPrintService(
        PosHardwareOptions hardware,
        IBarcodeService barcodes,
        IReceiptLogoProvider? logoProvider = null)
    {
        _hardware = hardware;
        _barcodes = barcodes;
        _logoProvider = logoProvider;
    }

    public string BuildReceiptText(Sale sale, ShopConfig config)
    {
        var sb = new StringBuilder();
        ReceiptFormatter.AppendHeader(sb, config);
        ReceiptFormatter.AppendInvoiceBody(sb, sale, config);
        sb.AppendLine(new string('-', 32));
        sb.AppendLine("[Scannable Code128 barcode prints on physical receipt]");
        sb.AppendLine();
        return sb.ToString();
    }

    public void PrintReceipt(Sale sale, ShopConfig config)
    {
        _currentSale = sale;
        _currentConfig = config;
        _printLines = BuildReceiptBody(sale, config)
            .Split('\n', StringSplitOptions.None)
            .ToList();
        _lineIndex = 0;
        _bodyComplete = false;
        _barcodeDrawn = false;

        var doc = new PrintDocument();
        doc.PrintPage += OnPrintPage;
        doc.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);

        try
        {
            doc.DefaultPageSettings.PaperSize = new PaperSize("Receipt80mm", 315, 1200);
        }
        catch
        {
            // Fall back to driver default paper size.
        }

        var printerName = ResolvePrinterName();
        if (!string.IsNullOrWhiteSpace(printerName))
            doc.PrinterSettings.PrinterName = printerName;

        doc.Print();

        if (_hardware.OpenCashDrawerOnPrint && !string.IsNullOrWhiteSpace(printerName))
        {
            try { EscPosPrinterHelper.OpenCashDrawer(printerName); }
            catch { /* drawer kick is best-effort */ }
        }
    }

    private string? ResolvePrinterName()
    {
        if (!string.IsNullOrWhiteSpace(_hardware.ThermalPrinterName))
            return _hardware.ThermalPrinterName;

        return PrinterSettings.InstalledPrinters.Count > 0
            ? PrinterSettings.InstalledPrinters[0]
            : null;
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        if (_currentSale is null || _currentConfig is null || e.Graphics is null)
            return;

        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        var pageWidth = (float)e.MarginBounds.Width;
        var left = (float)e.MarginBounds.Left;
        var y = (float)e.MarginBounds.Top;

        if (!_bodyComplete)
        {
            var maxY = e.MarginBounds.Bottom - BarcodeReservedHeight;

            if (_lineIndex == 0)
            {
                using var logo = TryLoadMonochromeLogo();
                if (logo is not null)
                {
                    var targetWidth = Math.Min(pageWidth * 0.65f, 200f);
                    var scale = targetWidth / logo.Width;
                    var targetHeight = logo.Height * scale;
                    var x = left + (pageWidth - targetWidth) / 2f;
                    g.DrawImage(logo, x, y, targetWidth, targetHeight);
                    y += targetHeight + 10f;
                }

                using var headerFont = new Font("Segoe UI", 9, FontStyle.Bold);
                using var subFont = new Font("Segoe UI", 8);
                var centerFormat = new StringFormat { Alignment = StringAlignment.Center };

                var (titleLine, subtitleLine) = ReceiptFormatter.GetHeaderLines(_currentConfig);
                g.DrawString(titleLine, headerFont, Brushes.Black,
                    new RectangleF(left, y, pageWidth, 22), centerFormat);
                y += 18f;
                g.DrawString(subtitleLine, subFont, Brushes.Black,
                    new RectangleF(left, y, pageWidth, 18), centerFormat);
                y += 22f;
            }

            using var bodyFont = new Font("Consolas", 8);
            var lineHeight = bodyFont.GetHeight(g) + 1f;

            while (_lineIndex < _printLines.Count && y + lineHeight <= maxY)
            {
                g.DrawString(_printLines[_lineIndex], bodyFont, Brushes.Black, left, y);
                y += lineHeight;
                _lineIndex++;
            }

            if (_lineIndex < _printLines.Count)
            {
                e.HasMorePages = true;
                return;
            }

            _bodyComplete = true;
        }

        if (!_barcodeDrawn)
        {
            y = Math.Max(y, e.MarginBounds.Top);
            DrawScannableBarcode(g, left, pageWidth, ref y);
            _barcodeDrawn = true;
        }

        e.HasMorePages = false;
    }

    private void DrawScannableBarcode(Graphics g, float left, float pageWidth, ref float y)
    {
        if (_currentSale is null)
            return;

        using var barcode = _barcodes.GenerateScannableBarcodeBitmap(_currentSale.ReceiptNo, 280, (int)BarcodeHeight);
        var targetWidth = Math.Min(pageWidth - 12f, barcode.Width);
        var targetHeight = BarcodeHeight;
        var barcodeX = left + (pageWidth - targetWidth) / 2f;

        y += 8f;
        g.DrawImage(barcode, barcodeX, y, targetWidth, targetHeight);
    }

    private static string BuildReceiptBody(Sale sale, ShopConfig config)
    {
        var sb = new StringBuilder();
        ReceiptFormatter.AppendInvoiceBody(sb, sale, config);
        return sb.ToString();
    }

    private Image? TryLoadMonochromeLogo()
    {
        if (_logoProvider is null)
            return null;

        Image? color = null;
        try
        {
            color = _logoProvider.GetColorLogo();
            return color is null ? null : ReceiptLogoConverter.ToMonochrome(color);
        }
        catch
        {
            return null;
        }
        finally
        {
            color?.Dispose();
        }
    }
}
