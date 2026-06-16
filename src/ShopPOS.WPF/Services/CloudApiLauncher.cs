using System.Diagnostics;
using System.IO;
using System.Net.Http;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.Services;

public static class CloudApiLauncher
{
    public static async Task EnsureRunningAsync(CloudSyncOptions options, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiBaseUrl))
            return;

        if (await IsReachableAsync(options, cancellationToken))
            return;

        var apiExe = LocateApiExecutable();
        if (apiExe is null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = apiExe,
                WorkingDirectory = Path.GetDirectoryName(apiExe)!,
                UseShellExecute = true
            });

            for (var attempt = 0; attempt < 15; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                if (await IsReachableAsync(options, cancellationToken))
                    return;
            }
        }
        catch
        {
            // Non-fatal: user can start API manually.
        }
    }

    private static async Task<bool> IsReachableAsync(CloudSyncOptions options, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{options.ApiBaseUrl.TrimEnd('/')}/");
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                request.Headers.Add("X-Api-Key", options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string? LocateApiExecutable()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "ShopPOS.Api.exe"),
            Path.Combine(baseDir, "..", "..", "..", "..", "ShopPOS.Api", "bin", "Debug", "net10.0", "ShopPOS.Api.exe"),
            Path.Combine(baseDir, "..", "..", "..", "..", "ShopPOS.Api", "bin", "Release", "net10.0", "ShopPOS.Api.exe")
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(full))
                return full;
        }

        return null;
    }
}
