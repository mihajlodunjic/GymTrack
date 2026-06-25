using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using GymTrack.Services;
using MediatR;

namespace GymTrack.Application.QrCodes;

public sealed record GenerateQrCodeForMemberQuery(int MemberId) : IRequest<byte[]>;

public sealed class GenerateQrCodeForMemberQueryHandler : IRequestHandler<GenerateQrCodeForMemberQuery, byte[]>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IQrCodeService _qrCodeService;

    public GenerateQrCodeForMemberQueryHandler(IMemberRepository memberRepository, IQrCodeService qrCodeService)
    {
        _memberRepository = memberRepository;
        _qrCodeService = qrCodeService;
    }

    public async Task<byte[]> Handle(GenerateQrCodeForMemberQuery query, CancellationToken cancellationToken)
    {
        var membershipCode = await _memberRepository.GetMembershipCodeAsync(query.MemberId, cancellationToken);
        if (membershipCode is null)
        {
            throw new NotFoundException($"Member with id '{query.MemberId}' was not found.");
        }

        return _qrCodeService.GenerateQrCodeFromText(membershipCode);
    }
}

public sealed record GenerateQrCodeForCurrentMemberQuery(ClaimsPrincipal Principal) : IRequest<byte[]>;

public sealed class GenerateQrCodeForCurrentMemberQueryHandler : IRequestHandler<GenerateQrCodeForCurrentMemberQuery, byte[]>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IQrCodeService _qrCodeService;

    public GenerateQrCodeForCurrentMemberQueryHandler(IMemberRepository memberRepository, IQrCodeService qrCodeService)
    {
        _memberRepository = memberRepository;
        _qrCodeService = qrCodeService;
    }

    public async Task<byte[]> Handle(GenerateQrCodeForCurrentMemberQuery query, CancellationToken cancellationToken)
    {
        var memberId = query.Principal.GetRequiredMemberId();
        var membershipCode = await _memberRepository.GetMembershipCodeAsync(memberId, cancellationToken);
        if (membershipCode is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        return _qrCodeService.GenerateQrCodeFromText(membershipCode);
    }
}
