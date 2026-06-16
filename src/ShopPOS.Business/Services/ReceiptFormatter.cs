using System.Text;
using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public static class ReceiptFormatter
{
    public static IEnumerable<ReceiptAmendmentLine> GetAmendmentLines(Sale sale)
    {
        if (sale.Amendments.Count > 0)
        {
            foreach (var amendment in sale.Amendments.OrderBy(a => a.AmendedAt))
            {
                yield return new ReceiptAmendmentLine(
                    amendment.Action,
                    amendment.ProductName,
                    amendment.Quantity,
                    amendment.AmendedAt,
                    amendment.AmountDelta);
            }
            yield break;
        }

        foreach (var ret in sale.Returns.OrderBy(r => r.ReturnDate))
        {
            yield return new ReceiptAmendmentLine(
                AmendmentAction.Return,
                ret.ProductName,
                ret.Quantity,
                ret.ReturnDate,
                -ret.RefundAmount);
        }
    }

    public static decimal GetNetTotal(Sale sale) =>
        sale.NetTotal > 0 || sale.ReturnedAmount > 0 || sale.AddedAmount > 0
            ? sale.NetTotal
            : sale.Total - sale.ReturnedAmount + sale.AddedAmount;

    public static IEnumerable<SaleItem> GetOriginalItems(Sale sale) =>
        sale.Items.Where(i => !i.IsAmendmentLine).OrderBy(i => i.Id);

    public static (string TitleLine, string SubtitleLine) GetHeaderLines(ShopConfig config)
    {
        var title = string.IsNullOrWhiteSpace(config.ShopName)
            ? BrandDisplay.TitleLine
            : config.ShopName;

        var subtitle = !string.IsNullOrWhiteSpace(config.Address) && !string.IsNullOrWhiteSpace(config.Phone)
            ? $"{config.Address} | {config.Phone}"
            : BrandDisplay.SubtitleLine;

        return (title, subtitle);
    }

    public static void AppendHeader(StringBuilder sb, ShopConfig config)
    {
        var (title, subtitle) = GetHeaderLines(config);
        sb.AppendLine(title);
        sb.AppendLine(subtitle);
    }

    public static void AppendInvoiceBody(StringBuilder sb, Sale sale, ShopConfig config)
    {
        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Invoice: {sale.ReceiptNo}");
        sb.AppendLine($"Date: {sale.SaleDate:g}");
        sb.AppendLine($"Cashier: {sale.SoldByUsername}");
        sb.AppendLine(new string('-', 32));

        sb.AppendLine("ORIGINAL INVOICE DETAILS");
        sb.AppendLine(new string('-', 32));
        foreach (var item in GetOriginalItems(sale))
        {
            sb.AppendLine(item.ProductName);
            sb.AppendLine($"  {item.Quantity} x Rs.{item.UnitPriceAtSale:N2} = Rs.{item.LineTotal:N2}");
        }

        var amendments = GetAmendmentLines(sale).ToList();
        if (amendments.Count > 0)
        {
            sb.AppendLine(new string('-', 32));
            sb.AppendLine("AMENDMENTS & RETURNS LOG");
            sb.AppendLine(new string('-', 32));
            foreach (var entry in amendments)
            {
                var actionLabel = entry.Action == AmendmentAction.Add ? "ADDED" : "RETURNED";
                sb.AppendLine($"{actionLabel}: {entry.ProductName} x{entry.Quantity}");
                sb.AppendLine($"  {entry.AmendedAt:g}  {FormatDelta(entry.AmountDelta)}");
            }
        }

        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Subtotal:    Rs.{sale.Subtotal,10:N2}");
        if (sale.DiscountAmount > 0)
            sb.AppendLine($"Discount:   -Rs.{sale.DiscountAmount,9:N2}");
        if (sale.TaxAmount > 0)
            sb.AppendLine($"Tax:         Rs.{sale.TaxAmount,10:N2}");
        sb.AppendLine($"ORIGINAL TOTAL: Rs.{sale.Total,10:N2}");

        if (sale.ReturnedAmount > 0)
            sb.AppendLine($"Returns:    -Rs.{sale.ReturnedAmount,9:N2}");
        if (sale.AddedAmount > 0)
            sb.AppendLine($"Additions:  +Rs.{sale.AddedAmount,9:N2}");

        sb.AppendLine($"NET TOTAL:   Rs.{GetNetTotal(sale),10:N2}");
        sb.AppendLine($"Payment: {sale.PaymentMethod.ToDisplayName()}");
        if (sale.PaymentMethod == PaymentMethod.Cash)
        {
            sb.AppendLine($"Received:    Rs.{sale.AmountReceived,10:N2}");
            sb.AppendLine($"Change:      Rs.{sale.ChangeAmount,10:N2}");
        }

        sb.AppendLine(new string('-', 32));
        sb.AppendLine(config.ReceiptFooter);
    }

    private static string FormatDelta(decimal amountDelta) =>
        amountDelta >= 0 ? $"+Rs.{amountDelta:N2}" : $"-Rs.{Math.Abs(amountDelta):N2}";
}

public readonly record struct ReceiptAmendmentLine(
    AmendmentAction Action,
    string ProductName,
    int Quantity,
    DateTime AmendedAt,
    decimal AmountDelta);
