using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;

namespace ShopPOS.Business.Services;

public interface IBarcodeService
{
    byte[] GenerateBarcodePng(string code, int width = 300, int height = 80);
    Bitmap GenerateBarcodeBitmap(string code, int width = 260, int height = 64);
    Bitmap GenerateScannableBarcodeBitmap(string code, int width = 280, int height = 50);
}

public class BarcodeService : IBarcodeService
{
    public byte[] GenerateBarcodePng(string code, int width = 300, int height = 80)
    {
        using var bitmap = GenerateScannableBarcodeBitmap(code, width, height);
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public Bitmap GenerateBarcodeBitmap(string code, int width = 260, int height = 64) =>
        GenerateScannableBarcodeBitmap(code, width, height);

    public Bitmap GenerateScannableBarcodeBitmap(string code, int width = 280, int height = 50)
    {
        var payload = code.Trim().ToUpperInvariant();
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 4,
                PureBarcode = true
            }
        };

        var pixelData = writer.Write(payload);
        using var color = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
        var bd = color.LockBits(
            new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bd.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            color.UnlockBits(bd);
        }

        return ToPureMonochrome(color);
    }

    internal static Bitmap ToPureMonochrome(Image source)
    {
        var width = source.Width;
        var height = source.Height;
        var mono = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using (var g = Graphics.FromImage(mono))
        {
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(source, 0, 0, width, height);
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = mono.GetPixel(x, y);
                var luminance = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                mono.SetPixel(x, y, luminance < 140 ? Color.Black : Color.White);
            }
        }

        return mono;
    }
}
