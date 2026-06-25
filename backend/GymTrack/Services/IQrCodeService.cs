namespace GymTrack.Services;

public interface IQrCodeService
{
    byte[] GenerateQrCodeFromText(string text);
}
