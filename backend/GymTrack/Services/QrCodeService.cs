using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.Security;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GymTrack.Services;

public sealed class QrCodeService : IQrCodeService
{
    private readonly AppDbContext _dbContext;

    public QrCodeService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    public async Task<byte[]> GenerateQrCodeForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var membershipCode = await GetMembershipCodeForMemberAsync(memberId, cancellationToken);
        return GenerateQrCodeFromText(membershipCode);
    }

    public async Task<byte[]> GenerateQrCodeForCurrentMemberAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var memberId = principal.GetRequiredMemberId();
        return await GenerateQrCodeForMemberAsync(memberId, cancellationToken);
    }

    private async Task<string> GetMembershipCodeForMemberAsync(int memberId, CancellationToken cancellationToken)
    {
        var membershipCode = await _dbContext.Members
            .AsNoTracking()
            .Where(member => member.Id == memberId)
            .Select(member => member.MembershipCode)
            .SingleOrDefaultAsync(cancellationToken);

        if (membershipCode is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        return membershipCode;
    }
}
