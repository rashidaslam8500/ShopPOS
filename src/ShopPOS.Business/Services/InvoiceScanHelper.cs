using System.Text.RegularExpressions;

namespace ShopPOS.Business.Services;

public static partial class InvoiceScanHelper
{
    [GeneratedRegex(@"BG\d{8}-\d{4}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReceiptPattern();

    public static string Normalize(string? scan)
    {
        if (string.IsNullOrWhiteSpace(scan))
            return string.Empty;

        var trimmed = scan.Trim().TrimEnd('\r', '\n');
        var match = ReceiptPattern().Match(trimmed);
        if (match.Success)
            return match.Value.ToUpperInvariant();

        // Some scanners strip punctuation; rebuild BG########-#### when possible.
        var compact = new string(trimmed.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant();
        if (compact.StartsWith("BG", StringComparison.Ordinal) && compact.Length >= 14)
        {
            var digits = compact[2..];
            if (digits.Length >= 12)
                return $"BG{digits[..8]}-{digits[8..12]}";
        }

        return trimmed.ToUpperInvariant();
    }

    public static bool LooksLikeInvoice(string? scan) =>
        !string.IsNullOrWhiteSpace(scan) && ReceiptPattern().IsMatch(scan.Trim());
}
