using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using ShopPOS.Domain.Interfaces;

using ShopPOS.Domain.Models;



namespace ShopPOS.Business.Services;



public interface ICloudSyncService

{

    Task SyncAsync(CancellationToken cancellationToken = default);

    DateTime? LastSuccessfulSyncUtc { get; }

    string? LastSyncStatus { get; }

}



public class CloudSyncService : ICloudSyncService

{

    private const string WatermarkKey = "Integration.CloudSync.LastWatermarkUtc";

    private const string LastStatusKey = "Integration.CloudSync.LastSyncStatus";



    private readonly HttpClient _http;

    private readonly CloudSyncOptions _options;

    private readonly ISaleRepository _sales;

    private readonly IProductRepository _products;

    private readonly ICustomerRepository _customers;

    private readonly ISettingsRepository _settings;

    private readonly ILogger<CloudSyncService>? _logger;

    private DateTime? _watermarkLoaded;

    private DateTime _lastSyncWatermarkUtc = DateTime.UtcNow.AddDays(-30);



    public DateTime? LastSuccessfulSyncUtc { get; private set; }

    public string? LastSyncStatus { get; private set; }



    public CloudSyncService(

        HttpClient http,

        CloudSyncOptions options,

        ISaleRepository sales,

        IProductRepository products,

        ICustomerRepository customers,

        ISettingsRepository settings,

        ILogger<CloudSyncService>? logger = null)

    {

        _http = http;

        _options = options;

        _sales = sales;

        _products = products;

        _customers = customers;

        _settings = settings;

        _logger = logger;

    }



    public async Task SyncAsync(CancellationToken cancellationToken = default)

    {

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiBaseUrl))

        {

            LastSyncStatus = "Cloud sync disabled.";

            await PersistStatusAsync(LastSyncStatus);

            return;

        }



        await EnsureWatermarkLoadedAsync();



        try

        {

            var since = _lastSyncWatermarkUtc;

            var sales = await _sales.GetSalesSinceAsync(since);

            var products = await _products.GetAllAsync();

            var customers = await _customers.GetUpdatedSinceAsync(since);



            var payload = new

            {

                shopId = _options.ShopId,

                syncedAtUtc = DateTime.UtcNow,

                sales = sales.Select(s => new

                {

                    s.Id,

                    s.ReceiptNo,

                    s.SaleDate,

                    s.Subtotal,

                    s.DiscountAmount,

                    s.TaxAmount,

                    s.Total,

                    s.NetTotal,

                    s.PaymentMethod,

                    s.CustomerPhone,

                    s.CustomerEmail,

                    s.Status,

                    items = s.Items.Select(i => new

                    {

                        i.ProductId,

                        i.ProductName,

                        i.Quantity,

                        i.UnitPriceAtSale,

                        i.LineTotal

                    })

                }),

                inventory = products.Select(p => new

                {

                    p.Id,

                    p.Name,

                    p.Category,

                    p.Price,

                    p.Stock,

                    p.Barcode,

                    p.Description,

                    p.UpdatedAt

                }),

                customers = customers.Select(c => new

                {

                    c.Id,

                    c.Phone,

                    c.Name,

                    c.FirstVisit,

                    c.LastVisit,

                    c.VisitCount

                })

            };



            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiBaseUrl.TrimEnd('/')}/sync");

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))

                request.Headers.Add("X-Api-Key", _options.ApiKey);



            request.Content = JsonContent.Create(payload);

            var response = await _http.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            _lastSyncWatermarkUtc = sales.Count > 0
                ? sales.Max(s => DateTime.SpecifyKind(s.SaleDate, DateTimeKind.Local).ToUniversalTime())
                : DateTime.UtcNow;

            LastSuccessfulSyncUtc = _lastSyncWatermarkUtc;

            LastSyncStatus = $"Synced {sales.Count} sale(s), {products.Count} product(s), {customers.Count} customer(s).";

            await _settings.SaveSettingAsync(WatermarkKey, _lastSyncWatermarkUtc.ToString("O"));

            await PersistStatusAsync(LastSyncStatus);

        }

        catch (Exception ex)

        {

            LastSyncStatus = $"Sync failed: {ex.Message}";

            await PersistStatusAsync(LastSyncStatus);

            _logger?.LogWarning(ex, "Cloud sync failed");

        }

    }



    private async Task PersistStatusAsync(string? status)

    {

        if (string.IsNullOrWhiteSpace(status))

            return;

        await _settings.SaveSettingAsync(LastStatusKey, status);

    }



    private async Task EnsureWatermarkLoadedAsync()

    {

        if (_watermarkLoaded.HasValue)

            return;



        var stored = await _settings.GetSettingAsync(WatermarkKey);

        if (DateTime.TryParse(stored, out var watermark))

            _lastSyncWatermarkUtc = watermark;



        _watermarkLoaded = DateTime.UtcNow;

    }

}


