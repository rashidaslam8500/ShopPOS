using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShopPOS.Data.Security;
using ShopPOS.Domain.Models;

namespace ShopPOS.Business.Services;

public interface IFingerprintScannerService : IDisposable
{
    bool IsDeviceConnected { get; }
    bool IsListening { get; }
    event EventHandler<FingerprintScanEventArgs>? FingerprintScanned;

    Task<FingerprintCaptureResult> CaptureEnrollmentAsync(CancellationToken cancellationToken = default);
    Task<int?> IdentifyWorkerAsync(string scannedTemplate, CancellationToken cancellationToken = default);
    void StartListening();
    void StopListening();
    string ProtectTemplate(string plainTemplate);
    string UnprotectTemplate(string storedTemplate);
}

public sealed class FingerprintScanEventArgs : EventArgs
{
    public string TemplateBase64 { get; init; } = string.Empty;
    public int? MatchedWorkerId { get; init; }
}

/// <summary>
/// USB fingerprint scanner wrapper with hooks for DigitalPersona / SecuGen SDKs.
/// Falls back to simulated capture when no hardware SDK is present.
/// </summary>
public class FingerprintScannerService : IFingerprintScannerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FingerprintScannerService>? _logger;
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;

    public bool IsDeviceConnected { get; private set; }
    public bool IsListening => _listenCts is not null;
    public event EventHandler<FingerprintScanEventArgs>? FingerprintScanned;

    public FingerprintScannerService(IServiceScopeFactory scopeFactory, ILogger<FingerprintScannerService>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        IsDeviceConnected = TryDetectUsbDevice();
    }

    public async Task<FingerprintCaptureResult> CaptureEnrollmentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await CaptureFromDeviceAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(template))
                return new FingerprintCaptureResult { Success = false, Message = "Fingerprint capture cancelled or failed." };

            return new FingerprintCaptureResult
            {
                Success = true,
                TemplateBase64 = ProtectTemplate(template),
                Message = "Thumbprint registered successfully."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fingerprint enrollment failed");
            return new FingerprintCaptureResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<int?> IdentifyWorkerAsync(string scannedTemplate, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var workers = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.IWorkerRepository>();
        var activeWorkers = await workers.GetAllAsync();

        foreach (var worker in activeWorkers.Where(w => !string.IsNullOrWhiteSpace(w.FingerprintTemplate)))
        {
            var stored = UnprotectTemplate(worker.FingerprintTemplate!);
            if (TemplatesMatch(stored, scannedTemplate))
                return worker.Id;
        }

        return null;
    }

    public void StartListening()
    {
        if (IsListening)
            return;

        _listenCts = new CancellationTokenSource();
        var token = _listenCts.Token;
        _listenTask = Task.Run(() => ListenLoopAsync(token), token);
    }

    public void StopListening()
    {
        _listenCts?.Cancel();
        _listenCts = null;
    }

    public string ProtectTemplate(string plainTemplate) =>
        SettingsSecretProtector.Protect(plainTemplate);

    public string UnprotectTemplate(string storedTemplate) =>
        SettingsSecretProtector.IsProtected(storedTemplate)
            ? SettingsSecretProtector.Unprotect(storedTemplate)
            : storedTemplate;

    public void Dispose()
    {
        StopListening();
        _listenCts?.Dispose();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Fingerprint listener started (DeviceConnected={Connected})", IsDeviceConnected);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (IsDeviceConnected)
                {
                    var template = await TryReadFromSdkAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(template))
                        await RaiseScanAsync(template, cancellationToken);
                }

                await Task.Delay(IsDeviceConnected ? 300 : 1500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Fingerprint listen tick error");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task RaiseScanAsync(string template, CancellationToken cancellationToken)
    {
        var workerId = await IdentifyWorkerAsync(template, cancellationToken);
        FingerprintScanned?.Invoke(this, new FingerprintScanEventArgs
        {
            TemplateBase64 = template,
            MatchedWorkerId = workerId
        });
    }

    private static bool TryDetectUsbDevice()
    {
        // Hook point: probe DigitalPersona OneTouch / SecuGen Hamster via native SDK.
        // Returns false when SDK DLLs are not installed — simulated mode still works for enrollment UI.
        return false;
    }

    private static async Task<string?> TryReadFromSdkAsync(CancellationToken cancellationToken)
    {
        // Hook point: native SDK capture callback → return template Base64.
        await Task.CompletedTask;
        return null;
    }

    private static async Task<string?> CaptureFromDeviceAsync(CancellationToken cancellationToken)
    {
        if (TryDetectUsbDevice())
        {
            var sdkTemplate = await TryReadFromSdkAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(sdkTemplate))
                return sdkTemplate;
        }

        // Simulated capture for development / when USB SDK is unavailable.
        await Task.Delay(1200, cancellationToken);
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static bool TemplatesMatch(string stored, string scanned) =>
        string.Equals(stored, scanned, StringComparison.Ordinal);

    /// <summary>Allows attendance panel to inject a simulated scan during SDK setup.</summary>
    public async Task SimulateScanAsync(string templateBase64, CancellationToken cancellationToken = default)
    {
        await RaiseScanAsync(templateBase64, cancellationToken);
    }
}
