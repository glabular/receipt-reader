using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;
using ZXing.Common;

namespace ReceiptReader.Services;

internal sealed class QrCodeReader
{
    public static string ReadQrCode(Stream imageStream)
    {
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        using var bitmap = SKBitmap.Decode(imageStream);

        if (bitmap == null)
        {
            Console.WriteLine("DEBUG: SkiaSharp could not decode the image. Stream length: " + imageStream.Length);
            
            return string.Empty;
        }

        var reader = new BarcodeReader()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        var result = reader.Decode(bitmap);

        return result?.Text ?? string.Empty;
    }
}
