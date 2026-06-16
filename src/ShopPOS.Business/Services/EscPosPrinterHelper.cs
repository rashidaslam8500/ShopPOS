using System.Runtime.InteropServices;

namespace ShopPOS.Business.Services;

/// <summary>
/// Sends raw ESC/POS bytes to a Windows printer (cash drawer kick, etc.).
/// </summary>
public static class EscPosPrinterHelper
{
    private static readonly byte[] CashDrawerPulse = [0x1B, 0x70, 0x00, 0x19, 0xFA];

    public static void OpenCashDrawer(string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return;

        SendBytes(printerName, CashDrawerPulse);
    }

    public static void SendBytes(string printerName, byte[] bytes)
    {
        if (bytes.Length == 0)
            return;

        if (!OpenPrinter(printerName, out var handle, IntPtr.Zero))
            throw new InvalidOperationException($"Could not open printer '{printerName}'.");

        try
        {
            var docInfo = new DocInfo
            {
                DocName = "ShopPOS RAW",
                DataType = "RAW"
            };

            if (!StartDocPrinter(handle, 1, docInfo))
                throw new InvalidOperationException("Could not start raw print job.");

            try
            {
                if (!StartPagePrinter(handle))
                    throw new InvalidOperationException("Could not start raw print page.");

                if (!WritePrinter(handle, bytes, bytes.Length, out _))
                    throw new InvalidOperationException("Could not write raw bytes to printer.");

                EndPagePrinter(handle);
            }
            finally
            {
                EndDocPrinter(handle);
            }
        }
        finally
        {
            ClosePrinter(handle);
        }
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern bool OpenPrinter(string? pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true, ExactSpelling = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DocInfo di);

    [DllImport("winspool.drv", SetLastError = true, ExactSpelling = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, ExactSpelling = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, ExactSpelling = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true, ExactSpelling = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string DocName = string.Empty;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? OutputFile;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string DataType = "RAW";
    }
}
