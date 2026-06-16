using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Enums;
using ShopPOS.Domain.Interfaces;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface ISaleService
{
    SaleTotals CalculateTotals(IEnumerable<CartItem> cart, decimal discountAmount, decimal taxRate);
    Task<Sale> CompleteSaleAsync(IEnumerable<CartItem> cart, decimal discountAmount, decimal taxRate, PaymentMethod paymentMethod, decimal amountReceived, string? customerPhone = null, string? customerEmail = null);
    Task<Sale> ProcessCorrectAndProceedAsync(int saleId, IEnumerable<ReturnRequest> returns, IEnumerable<AddItemRequest> additions);
    Task<Sale> ProcessReturnsAsync(int saleId, IEnumerable<ReturnRequest> returns);
    Task<IReadOnlyList<Sale>> GetSalesAsync(DateTime? date = null);
    Task<Sale?> GetSaleAsync(int id);
    Task<Sale?> GetByReceiptNoAsync(string receiptNo);
    Task<IReadOnlyList<Sale>> GetDeletedSalesAsync();
    Task SoftDeleteSaleAsync(int saleId);
    Task RestoreSalesAsync(IEnumerable<int> saleIds);
    Task PermanentDeleteSalesAsync(IEnumerable<int> saleIds, string ownerPassword);
}

public class SaleService : ISaleService
{
    private readonly ISaleRepository _sales;
    private readonly IProductRepository _products;
    private readonly CurrentSession _session;
    private readonly IAuditService _audit;
    private readonly ICustomerRepository _customers;
    private readonly IAuthService _auth;

    public SaleService(
        ISaleRepository sales,
        IProductRepository products,
        ICustomerRepository customers,
        CurrentSession session,
        IAuditService audit,
        IAuthService auth)
    {
        _sales = sales;
        _products = products;
        _customers = customers;
        _session = session;
        _audit = audit;
        _auth = auth;
    }

    public SaleTotals CalculateTotals(IEnumerable<CartItem> cart, decimal discountAmount, decimal taxRate)
    {
        var items = cart.ToList();
        var subtotal = items.Sum(i => i.LineTotal);
        var discount = Math.Clamp(discountAmount, 0, subtotal);
        var afterDiscount = subtotal - discount;
        var taxAmount = afterDiscount * (taxRate / 100m);
        return new SaleTotals
        {
            Subtotal = subtotal,
            DiscountPercent = subtotal > 0 ? Math.Round(discount / subtotal * 100m, 2) : 0,
            DiscountAmount = discount,
            TaxAmount = taxAmount,
            Total = afterDiscount + taxAmount
        };
    }

    public async Task<Sale> CompleteSaleAsync(
        IEnumerable<CartItem> cart,
        decimal discountAmount,
        decimal taxRate,
        PaymentMethod paymentMethod,
        decimal amountReceived,
        string? customerPhone = null,
        string? customerEmail = null)
    {
        if (_session.User is null)
            throw new InvalidOperationException("Not authenticated.");

        var items = cart.ToList();
        if (items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        foreach (var item in items)
        {
            var product = await _products.GetByIdAsync(item.ProductId)
                ?? throw new InvalidOperationException($"Product not found: {item.Name}");
            if (product.Stock < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for {item.Name}.");
        }

        var totals = CalculateTotals(items, discountAmount, taxRate);
        if (paymentMethod == PaymentMethod.Cash && amountReceived < totals.Total)
            throw new InvalidOperationException("Insufficient amount received.");

        Sale? saved = null;
        await _sales.RunInTransactionAsync(async () =>
        {
            var seq = await _sales.GetNextReceiptSequenceAsync();
            var sale = new Sale
            {
                ReceiptNo = $"BG{DateTime.Now:yyyyMMdd}-{seq:D4}",
                Subtotal = totals.Subtotal,
                DiscountPercent = totals.DiscountPercent,
                DiscountAmount = totals.DiscountAmount,
                TaxAmount = totals.TaxAmount,
                Total = totals.Total,
                NetTotal = totals.Total,
                AddedAmount = 0,
                PaymentMethod = paymentMethod,
                AmountReceived = paymentMethod == PaymentMethod.Cash ? amountReceived : totals.Total,
                ChangeAmount = paymentMethod == PaymentMethod.Cash ? Math.Max(0, amountReceived - totals.Total) : 0,
                SoldByUserId = _session.User!.UserId,
                SoldByUsername = _session.User.Username,
                SaleDate = DateTime.Now,
                Status = InvoiceStatus.Completed,
                CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : PhoneHelper.Normalize(customerPhone),
                CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
                Items = items.Select(i => new SaleItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Name,
                    UnitPriceAtSale = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal,
                    IsAmendmentLine = false
                }).ToList()
            };

            foreach (var item in items)
                await _products.AdjustStockAsync(item.ProductId, -item.Quantity);

            saved = await _sales.AddAsync(sale);

            if (!string.IsNullOrWhiteSpace(saved.CustomerPhone))
                await _customers.UpsertVisitAsync(saved.CustomerPhone);
        });

        await _audit.LogAsync(AuditActionType.SaleCreated, "Sale", saved!.Id.ToString(),
            $"Invoice {saved.ReceiptNo} — Total Rs.{saved.Total:N2} — {saved.Items.Count} item(s)",
            newValues: $"{{\"total\":{saved.Total},\"items\":{saved.Items.Count}}}");

        return saved;
    }

    public Task<Sale> ProcessReturnsAsync(int saleId, IEnumerable<ReturnRequest> returns) =>
        ProcessCorrectAndProceedAsync(saleId, returns, []);

    public async Task<Sale> ProcessCorrectAndProceedAsync(
        int saleId,
        IEnumerable<ReturnRequest> returns,
        IEnumerable<AddItemRequest> additions)
    {
        if (_session.User is null)
            throw new InvalidOperationException("Not authenticated.");

        var returnList = returns.ToList();
        var additionList = additions.ToList();
        if (returnList.Count == 0 && additionList.Count == 0)
            throw new InvalidOperationException("No returns or additions specified.");

        var sale = await _sales.GetByIdAsync(saleId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var now = DateTime.Now;
        decimal totalRefund = 0;
        decimal totalAdded = 0;

        await _sales.RunInTransactionAsync(async () =>
        {
            foreach (var req in returnList)
            {
                var line = sale.Items.FirstOrDefault(i => i.Id == req.SaleItemId && !i.IsAmendmentLine)
                    ?? throw new InvalidOperationException($"Invoice line {req.SaleItemId} not found.");

                if (req.Quantity <= 0 || req.Quantity > line.RemainingQuantity)
                    throw new InvalidOperationException($"Invalid return quantity for {line.ProductName}.");

                var refund = line.UnitPriceAtSale * req.Quantity;
                totalRefund += refund;
                line.ReturnedQuantity += req.Quantity;

                var saleReturn = new SaleReturn
                {
                    SaleId = sale.Id,
                    SaleItemId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = req.Quantity,
                    UnitPriceAtOriginalSale = line.UnitPriceAtSale,
                    RefundAmount = refund,
                    Reason = req.Reason,
                    ProcessedByUserId = _session.User!.UserId,
                    ProcessedByUsername = _session.User.Username,
                    ReturnDate = now
                };

                var amendment = new SaleAmendment
                {
                    SaleId = sale.Id,
                    SaleItemId = line.Id,
                    Action = AmendmentAction.Return,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    Quantity = req.Quantity,
                    UnitPrice = line.UnitPriceAtSale,
                    AmountDelta = -refund,
                    Reason = req.Reason,
                    ProcessedByUserId = _session.User.UserId,
                    ProcessedByUsername = _session.User.Username,
                    AmendedAt = now
                };

                await _sales.StageReturnAsync(saleReturn);
                await _sales.StageAmendmentAsync(amendment);
                await _products.AdjustStockAsync(line.ProductId, req.Quantity);
            }

            foreach (var req in additionList)
            {
                if (req.Quantity <= 0)
                    throw new InvalidOperationException("Addition quantity must be greater than zero.");

                var product = await _products.GetByIdAsync(req.ProductId)
                    ?? throw new InvalidOperationException($"Product {req.ProductId} not found.");

                if (product.Stock < req.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}.");

                var lineTotal = product.Price * req.Quantity;
                totalAdded += lineTotal;

                var newLine = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPriceAtSale = product.Price,
                    Quantity = req.Quantity,
                    LineTotal = lineTotal,
                    IsAmendmentLine = true
                };

                await _sales.StageItemAsync(newLine);

                var amendment = new SaleAmendment
                {
                    SaleId = sale.Id,
                    Action = AmendmentAction.Add,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = req.Quantity,
                    UnitPrice = product.Price,
                    AmountDelta = lineTotal,
                    Reason = req.Reason,
                    ProcessedByUserId = _session.User!.UserId,
                    ProcessedByUsername = _session.User.Username,
                    AmendedAt = now
                };

                await _sales.StageAmendmentAsync(amendment);
                await _products.AdjustStockAsync(product.Id, -req.Quantity);
            }

            sale.ReturnedAmount += totalRefund;
            sale.AddedAmount += totalAdded;
            sale.NetTotal = sale.Total - sale.ReturnedAmount + sale.AddedAmount;
            sale.Status = ResolveStatus(sale, returnList.Count > 0, additionList.Count > 0);
        });

        await _audit.LogAsync(AuditActionType.SaleAmended, "Sale", sale.Id.ToString(),
            $"Invoice {sale.ReceiptNo} amended — Returns Rs.{totalRefund:N2}, Additions Rs.{totalAdded:N2}, Net Rs.{sale.NetTotal:N2}",
            newValues: $"{{\"returns\":{totalRefund},\"additions\":{totalAdded},\"netTotal\":{sale.NetTotal}}}");

        return (await _sales.GetByIdAsync(saleId))!;
    }

    private static InvoiceStatus ResolveStatus(Sale sale, bool hasReturns, bool hasAdditions)
    {
        if (hasAdditions)
            return InvoiceStatus.Amended;

        return sale.Items.Where(i => !i.IsAmendmentLine).All(i => i.ReturnedQuantity >= i.Quantity)
            ? InvoiceStatus.FullReturn
            : InvoiceStatus.PartialReturn;
    }

    public Task<IReadOnlyList<Sale>> GetSalesAsync(DateTime? date = null) => _sales.GetAllAsync(date);
    public Task<Sale?> GetSaleAsync(int id) => _sales.GetByIdAsync(id);
    public Task<Sale?> GetByReceiptNoAsync(string receiptNo) => _sales.GetByReceiptNoAsync(receiptNo);
    public Task<IReadOnlyList<Sale>> GetDeletedSalesAsync() => _sales.GetDeletedAsync();

    public async Task SoftDeleteSaleAsync(int saleId)
    {
        if (_session.User is null)
            throw new InvalidOperationException("Not authenticated.");

        var sale = await _sales.GetByIdAsync(saleId)
            ?? throw new InvalidOperationException("Invoice not found.");

        sale.IsDeleted = true;
        sale.DeletedAt = DateTime.Now;
        sale.DeletedByUserId = _session.User.UserId;
        await _sales.UpdateAsync(sale);

        await _audit.LogAsync(AuditActionType.SaleDeleted, "Sale", sale.Id.ToString(),
            $"Invoice {sale.ReceiptNo} moved to trash");
    }

    public async Task RestoreSalesAsync(IEnumerable<int> saleIds)
    {
        foreach (var id in saleIds)
        {
            var sale = await _sales.GetByIdAsync(id, includeDeleted: true);
            if (sale is null || !sale.IsDeleted)
                continue;

            sale.IsDeleted = false;
            sale.DeletedAt = null;
            sale.DeletedByUserId = null;
            await _sales.UpdateAsync(sale);
        }
    }

    public async Task PermanentDeleteSalesAsync(IEnumerable<int> saleIds, string ownerPassword)
    {
        if (!await _auth.VerifyOwnerPasswordAsync(ownerPassword))
            throw new UnauthorizedAccessException("Owner password is incorrect.");

        foreach (var id in saleIds)
        {
            var sale = await _sales.GetByIdAsync(id, includeDeleted: true);
            if (sale is null || !sale.IsDeleted)
                continue;

            await _sales.PermanentDeleteAsync(id);
            await _audit.LogAsync(AuditActionType.SaleDeleted, "Sale", id.ToString(), "Invoice permanently purged");
        }
    }
}
