using System.Security.Claims;

namespace GymTrack.Services;

public interface IQrCodeService
{
    byte[] GenerateQrCodeFromText(string text);

    Task<byte[]> GenerateQrCodeForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    Task<byte[]> GenerateQrCodeForCurrentMemberAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
