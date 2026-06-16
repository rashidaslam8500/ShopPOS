using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ShopPOS.Business.Services;

internal static class ReceiptLogoConverter
{
    private const int ThermalMaxWidth = 384;

    public static Bitmap ToMonochrome(Image source)
    {
        var scale = source.Width > ThermalMaxWidth
            ? (float)ThermalMaxWidth / source.Width
            : 1f;
        var width = Math.Max(1, (int)(source.Width * scale));
        var height = Math.Max(1, (int)(source.Height * scale));

        using var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
        }

        var mono = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = scaled.GetPixel(x, y);
                var luminance = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                mono.SetPixel(x, y, luminance >= 140 ? Color.White : Color.Black);
            }
        }

        return mono;
    }
}
