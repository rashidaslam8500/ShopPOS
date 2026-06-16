using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IWhatsAppService
{
    Task SendThankYouAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppService>? _logger;

    public WhatsAppService(HttpClient http, WhatsAppOptions options, ILogger<WhatsAppService>? logger = null)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task SendThankYouAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var phone = NormalizePhone(phoneNumber);
        if (phone.Length < 10)
            return;

        try
        {
            if (_options.Provider.Equals("MetaCloud", StringComparison.OrdinalIgnoreCase))
            {
                await SendMetaCloudAsync(phone, _options.ThankYouMessage, cancellationToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_options.ApiUrl))
            {
                var payload = new
                {
                    to = phone,
                    message = _options.ThankYouMessage,
                    phoneNumberId = _options.PhoneNumberId,
                    apiKey = _options.ApiKey
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiUrl);
                request.Content = JsonContent.Create(payload);
                var response = await _http.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "WhatsApp send failed for {Phone}", phone);
        }
    }

    private async Task SendMetaCloudAsync(string phone, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PhoneNumberId) || string.IsNullOrWhiteSpace(_options.ApiKey))
            return;

        var url = string.IsNullOrWhiteSpace(_options.ApiUrl)
            ? $"https://graph.facebook.com/v19.0/{_options.PhoneNumberId}/messages"
            : _options.ApiUrl.TrimEnd('/');

        var to = phone.StartsWith("92") ? phone : $"92{phone.TrimStart('0')}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = message }
        });

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
