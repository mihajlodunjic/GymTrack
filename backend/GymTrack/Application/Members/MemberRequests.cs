using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.Member;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using GymTrack.Services;
using MediatR;

namespace GymTrack.Application.Members;

public sealed record GetAllMembersQuery(string? Search, bool? IsActive) : IRequest<IReadOnlyList<MemberResponse>>;

public sealed class GetAllMembersQueryHandler : IRequestHandler<GetAllMembersQuery, IReadOnlyList<MemberResponse>>
{
    private readonly IMemberRepository _memberRepository;

    public GetAllMembersQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<IReadOnlyList<MemberResponse>> Handle(GetAllMembersQuery query, CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetAllAsync(query.Search, query.IsActive, cancellationToken);
        return members.Select(MemberRequestMappings.MapMemberResponse).ToArray();
    }
}

public sealed record GetMemberByIdQuery(int MemberId) : IRequest<MemberDetailsResponse>;

public sealed class GetMemberByIdQueryHandler : IRequestHandler<GetMemberByIdQuery, MemberDetailsResponse>
{
    private readonly IMemberRepository _memberRepository;

    public GetMemberByIdQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberDetailsResponse> Handle(GetMemberByIdQuery query, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(query.MemberId, true, cancellationToken);
        return member is null
            ? throw new NotFoundException($"Member with id '{query.MemberId}' was not found.")
            : MemberRequestMappings.MapMemberDetails(member);
    }
}

public sealed record GetMemberByCodeQuery(string MembershipCode) : IRequest<MemberDetailsResponse>;

public sealed class GetMemberByCodeQueryHandler : IRequestHandler<GetMemberByCodeQuery, MemberDetailsResponse>
{
    private readonly IMemberRepository _memberRepository;

    public GetMemberByCodeQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberDetailsResponse> Handle(GetMemberByCodeQuery query, CancellationToken cancellationToken)
    {
        var normalizedCode = MemberRequestMappings.NormalizeMembershipCode(query.MembershipCode);
        var member = await _memberRepository.GetByCodeWithUserAsync(normalizedCode, true, cancellationToken);

        return member is null
            ? throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.")
            : MemberRequestMappings.MapMemberDetails(member);
    }
}

public sealed record GetCurrentMemberProfileQuery(ClaimsPrincipal Principal) : IRequest<MemberDetailsResponse>;

public sealed class GetCurrentMemberProfileQueryHandler : IRequestHandler<GetCurrentMemberProfileQuery, MemberDetailsResponse>
{
    private readonly IMemberRepository _memberRepository;

    public GetCurrentMemberProfileQueryHandler(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberDetailsResponse> Handle(GetCurrentMemberProfileQuery query, CancellationToken cancellationToken)
    {
        var memberId = query.Principal.GetRequiredMemberId();
        var member = await _memberRepository.GetByIdWithUserAsync(memberId, true, cancellationToken);

        return member is null
            ? throw new NotFoundException($"Member with id '{memberId}' was not found.")
            : MemberRequestMappings.MapMemberDetails(member);
    }
}

public sealed record CreateMemberCommand(CreateMemberRequest Request) : IRequest<MemberDetailsResponse>;

public sealed class CreateMemberCommandHandler : IRequestHandler<CreateMemberCommand, MemberDetailsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public CreateMemberCommandHandler(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<MemberDetailsResponse> Handle(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = MemberRequestMappings.NormalizeEmail(command.Request.Email);

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");
        }

        var membershipCode = await GenerateMembershipCodeAsync(cancellationToken);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(command.Request.Password),
            Role = UserRole.Member,
            IsActive = true
        };

        var member = new Member
        {
            User = user,
            FirstName = command.Request.FirstName.Trim(),
            LastName = command.Request.LastName.Trim(),
            PhoneNumber = MemberRequestMappings.NormalizeOptionalValue(command.Request.PhoneNumber),
            MembershipCode = membershipCode,
            IsActive = true
        };

        _memberRepository.Add(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MemberRequestMappings.MapMemberDetails(member);
    }

    private async Task<string> GenerateMembershipCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"GYM-{year}-";
        var existingCodes = await _memberRepository.GetExistingMembershipCodesForYearPrefixAsync(prefix, cancellationToken);

        var nextNumber = existingCodes
            .Select(code => code[prefix.Length..])
            .Select(suffix => int.TryParse(suffix, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        string candidate;
        do
        {
            candidate = $"{prefix}{nextNumber:0000}";
            nextNumber++;
        }
        while (existingCodes.Contains(candidate, StringComparer.OrdinalIgnoreCase));

        return candidate;
    }
}

public sealed record UpdateMemberCommand(int MemberId, UpdateMemberRequest Request) : IRequest<MemberDetailsResponse>;

public sealed class UpdateMemberCommandHandler : IRequestHandler<UpdateMemberCommand, MemberDetailsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberCommandHandler(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberDetailsResponse> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(command.MemberId, false, cancellationToken);
        if (member is null)
        {
            throw new NotFoundException($"Member with id '{command.MemberId}' was not found.");
        }

        var normalizedEmail = MemberRequestMappings.NormalizeEmail(command.Request.Email);
        var emailTaken = await _userRepository.EmailExistsForOtherUserAsync(normalizedEmail, member.UserId, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");
        }

        member.FirstName = command.Request.FirstName.Trim();
        member.LastName = command.Request.LastName.Trim();
        member.PhoneNumber = MemberRequestMappings.NormalizeOptionalValue(command.Request.PhoneNumber);
        member.User.Email = normalizedEmail;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MemberRequestMappings.MapMemberDetails(member);
    }
}

public sealed record DeactivateMemberCommand(int MemberId) : IRequest;

public sealed class DeactivateMemberCommandHandler : IRequestHandler<DeactivateMemberCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateMemberCommandHandler(IMemberRepository memberRepository, IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(command.MemberId, false, cancellationToken);
        if (member is null)
        {
            throw new NotFoundException($"Member with id '{command.MemberId}' was not found.");
        }

        member.IsActive = false;
        member.User.IsActive = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

file static class MemberRequestMappings
{
    public static MemberResponse MapMemberResponse(Member member) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.User.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipCode = member.MembershipCode,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };

    public static MemberDetailsResponse MapMemberDetails(Member member) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.User.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipCode = member.MembershipCode,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public static string NormalizeMembershipCode(string membershipCode) =>
        membershipCode.Trim().ToUpperInvariant();

    public static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
