using System.Drawing;
using System.IO;
using ShopPOS.Business.Services;
using ShopPOS.Domain.Models;

namespace ShopPOS.WPF.Assets;

public sealed class WpfReceiptLogoProvider : IReceiptLogoProvider
{
    private readonly PosHardwareOptions _hardware;

    public WpfReceiptLogoProvider(PosHardwareOptions hardware) => _hardware = hardware;

    public Image? GetColorLogo()
    {
        try
        {
            using var stream = LogoHelper.OpenLogoStream(_hardware);
            if (stream is null)
                return null;

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            copy.Position = 0;
            return Image.FromStream(copy);
        }
        catch
        {
            return null;
        }
    }
}
