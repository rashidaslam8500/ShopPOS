using System.Drawing;

namespace ShopPOS.Business.Services;

public interface IReceiptLogoProvider
{
    Image? GetColorLogo();
}
