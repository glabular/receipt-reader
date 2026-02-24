using OpenCvSharp;

namespace ReceiptReader.Services;

internal class WeChatQrReader : IDisposable
{
    private readonly WeChatQRCode _detector;

    public WeChatQrReader()
    {
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "OpenCvSharp4");

        _detector = WeChatQRCode.Create(
            $"{modelPath}/detect.prototxt",
            $"{modelPath}/detect.caffemodel",
            $"{modelPath}/sr.prototxt",
            $"{modelPath}/sr.caffemodel"
        );
    }

    public string? ReadQr(string imagePath)
    {
        using var mat = Cv2.ImRead(imagePath);
        if (mat.Empty())
        {
            throw new Exception("Could not load image.");
        }

        _detector.DetectAndDecode(mat, out Mat[] points, out string[] results);

        // TODO: Return all detected QR codes instead of just the first one.
        return results.Length > 0 ? results[0] : null;
    }

    public void Dispose()
    {
        _detector?.Dispose();
    }
}
