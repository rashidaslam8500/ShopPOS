using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IEmailService
{
    Task SendThankYouAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService>? _logger;

    public EmailService(EmailOptions options, ILogger<EmailService>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendThankYouAsync(string emailAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var email = emailAddress.Trim();
        if (!email.Contains('@') || string.IsNullOrWhiteSpace(_options.SmtpHost))
            return;

        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);

            var from = string.IsNullOrWhiteSpace(_options.FromAddress)
                ? _options.Username
                : _options.FromAddress;

            using var message = new MailMessage
            {
                From = new MailAddress(from, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(email);

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Email send failed for {Email}", email);
        }
    }
}
