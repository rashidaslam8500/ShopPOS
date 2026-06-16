using ShopPOS.Domain.Entities;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface ICustomerNotificationService
{
    Task SendCheckoutThankYouAsync(Sale sale, ShopConfig config, string receiptText, CancellationToken cancellationToken = default);
}

public class CustomerNotificationService : ICustomerNotificationService
{
    private readonly ISmsService _sms;
    private readonly IWhatsAppService _whatsApp;
    private readonly IEmailService _email;
    private readonly SmsOptions _smsOptions;
    private readonly EmailOptions _emailOptions;

    public CustomerNotificationService(
        ISmsService sms,
        IWhatsAppService whatsApp,
        IEmailService email,
        SmsOptions smsOptions,
        EmailOptions emailOptions)
    {
        _sms = sms;
        _whatsApp = whatsApp;
        _email = email;
        _smsOptions = smsOptions;
        _emailOptions = emailOptions;
    }

    public async Task SendCheckoutThankYouAsync(
        Sale sale,
        ShopConfig config,
        string receiptText,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(sale.CustomerPhone))
        {
            await _sms.SendThankYouAsync(sale.CustomerPhone, cancellationToken);
            await _whatsApp.SendThankYouAsync(sale.CustomerPhone, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(sale.CustomerEmail))
        {
            var subject = _emailOptions.ThankYouSubject.Replace("{ReceiptNo}", sale.ReceiptNo, StringComparison.OrdinalIgnoreCase);
            var body = $"{_smsOptions.ThankYouMessage}\n\n--- Receipt {sale.ReceiptNo} ---\n{receiptText}";
            await _email.SendThankYouAsync(sale.CustomerEmail, subject, body, cancellationToken);
        }
    }
}
