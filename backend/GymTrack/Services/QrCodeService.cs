using GymTrack.Common.Exceptions;
using QRCoder;

namespace GymTrack.Services;

public sealed class QrCodeService : IQrCodeService
{
    public byte[] GenerateQrCodeFromText(string text)
    {
        var normalizedText = string.IsNullOrWhiteSpace(text)
            ? throw new BadRequestException("QR text is required.")
            : text.Trim();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(normalizedText, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(20);
    }
}
