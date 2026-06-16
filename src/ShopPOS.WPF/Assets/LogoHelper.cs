using System.IO;
using System.Windows.Media.Imaging;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.Assets;

public static class LogoHelper
{
    public const string PackUri = "pack://application:,,,/Assets/logo.png";
    public const string RelativeOutputPath = "Assets/logo.png";

    public static string[] GetCandidatePaths(PosHardwareOptions? hardwareOptions = null)
    {
        var configured = hardwareOptions?.LogoPath;
        return
        [
            configured ?? string.Empty,
            Path.Combine(AppContext.BaseDirectory, RelativeOutputPath)
        ];
    }

    public static Stream? OpenLogoStream(PosHardwareOptions? hardwareOptions = null)
    {
        foreach (var path in GetCandidatePaths(hardwareOptions))
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;

            try
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch
            {
                // Try next path.
            }
        }

        try
        {
            var streamInfo = System.Windows.Application.GetResourceStream(new Uri(PackUri, UriKind.Absolute));
            if (streamInfo?.Stream is null)
                return null;

            var copy = new MemoryStream();
            streamInfo.Stream.CopyTo(copy);
            copy.Position = 0;
            return copy;
        }
        catch
        {
            return null;
        }
    }

    public static BitmapImage CreateColorLogo(PosHardwareOptions? hardwareOptions = null)
    {
        using var stream = OpenLogoStream(hardwareOptions);
        if (stream is not null)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        var fallback = new BitmapImage();
        fallback.BeginInit();
        fallback.CacheOption = BitmapCacheOption.OnLoad;
        fallback.UriSource = new Uri(PackUri, UriKind.Absolute);
        fallback.EndInit();
        fallback.Freeze();
        return fallback;
    }
}
