using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface ISmsService
{
    Task SendThankYouAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

public class SmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsService>? _logger;

    public SmsService(HttpClient http, SmsOptions options, ILogger<SmsService>? logger = null)
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
            if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            {
                await SendTwilioAsync(phone, _options.ThankYouMessage, cancellationToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(_options.ApiUrl))
            {
                var payload = new
                {
                    to = phone,
                    message = _options.ThankYouMessage,
                    sender = _options.SenderId,
                    username = _options.ApiUsername,
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
            _logger?.LogWarning(ex, "SMS send failed for {Phone}", phone);
        }
    }

    private async Task SendTwilioAsync(string phone, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiUrl) || string.IsNullOrWhiteSpace(_options.ApiKey))
            return;

        var segments = _options.ApiKey.Split(':', 2);
        if (segments.Length != 2)
            return;

        var accountSid = segments[0];
        var authToken = segments[1];
        var url = $"{_options.ApiUrl.TrimEnd('/')}/2010-04-01/Accounts/{accountSid}/Messages.json";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = phone.StartsWith('+') ? phone : $"+92{phone.TrimStart('0')}",
            ["From"] = _options.SenderId,
            ["Body"] = message
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
