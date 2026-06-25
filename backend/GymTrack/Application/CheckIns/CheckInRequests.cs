using System.Security.Claims;
using GymTrack.Common;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.CheckIn;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using MediatR;

namespace GymTrack.Application.CheckIns;

public sealed record GetAllCheckInsQuery : IRequest<IReadOnlyList<CheckInResponse>>;

public sealed class GetAllCheckInsQueryHandler : IRequestHandler<GetAllCheckInsQuery, IReadOnlyList<CheckInResponse>>
{
    private readonly ICheckInRepository _checkInRepository;

    public GetAllCheckInsQueryHandler(ICheckInRepository checkInRepository)
    {
        _checkInRepository = checkInRepository;
    }

    public async Task<IReadOnlyList<CheckInResponse>> Handle(GetAllCheckInsQuery query, CancellationToken cancellationToken)
    {
        var checkIns = await _checkInRepository.GetAllWithDetailsAsync(cancellationToken);
        return checkIns
            .OrderByDescending(checkIn => checkIn.CheckedInAt)
            .ThenByDescending(checkIn => checkIn.Id)
            .Select(CheckInRequestMappings.MapResponse)
            .ToArray();
    }
}

public sealed record GetCheckInsForMemberQuery(int MemberId) : IRequest<IReadOnlyList<CheckInResponse>>;

public sealed class GetCheckInsForMemberQueryHandler : IRequestHandler<GetCheckInsForMemberQuery, IReadOnlyList<CheckInResponse>>
{
    private readonly IMemberRepository _memberRepository;
    private readonly ICheckInRepository _checkInRepository;

    public GetCheckInsForMemberQueryHandler(IMemberRepository memberRepository, ICheckInRepository checkInRepository)
    {
        _memberRepository = memberRepository;
        _checkInRepository = checkInRepository;
    }

    public async Task<IReadOnlyList<CheckInResponse>> Handle(GetCheckInsForMemberQuery query, CancellationToken cancellationToken)
    {
        if (!await _memberRepository.ExistsAsync(query.MemberId, cancellationToken))
        {
            throw new NotFoundException($"Member with id '{query.MemberId}' was not found.");
        }

        var checkIns = await _checkInRepository.GetForMemberWithDetailsAsync(query.MemberId, cancellationToken);
        return checkIns
            .OrderByDescending(checkIn => checkIn.CheckedInAt)
            .ThenByDescending(checkIn => checkIn.Id)
            .Select(CheckInRequestMappings.MapResponse)
            .ToArray();
    }
}

public sealed record GetCurrentMemberCheckInsQuery(ClaimsPrincipal Principal) : IRequest<IReadOnlyList<CheckInResponse>>;

public sealed class GetCurrentMemberCheckInsQueryHandler : IRequestHandler<GetCurrentMemberCheckInsQuery, IReadOnlyList<CheckInResponse>>
{
    private readonly IMemberRepository _memberRepository;
    private readonly ICheckInRepository _checkInRepository;

    public GetCurrentMemberCheckInsQueryHandler(IMemberRepository memberRepository, ICheckInRepository checkInRepository)
    {
        _memberRepository = memberRepository;
        _checkInRepository = checkInRepository;
    }

    public async Task<IReadOnlyList<CheckInResponse>> Handle(GetCurrentMemberCheckInsQuery query, CancellationToken cancellationToken)
    {
        var memberId = query.Principal.GetRequiredMemberId();

        if (!await _memberRepository.ExistsAsync(memberId, cancellationToken))
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        var checkIns = await _checkInRepository.GetForMemberWithDetailsAsync(memberId, cancellationToken);
        return checkIns
            .OrderByDescending(checkIn => checkIn.CheckedInAt)
            .ThenByDescending(checkIn => checkIn.Id)
            .Select(CheckInRequestMappings.MapResponse)
            .ToArray();
    }
}

public sealed record CreateCheckInByMemberIdCommand(int MemberId, CreateCheckInByMemberIdRequest Request, ClaimsPrincipal Principal) : IRequest<CheckInResponse>;

public sealed class CreateCheckInByMemberIdCommandHandler : IRequestHandler<CreateCheckInByMemberIdCommand, CheckInResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCheckInByMemberIdCommandHandler(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IMembershipPaymentRepository membershipPaymentRepository,
        ICheckInRepository checkInRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
        _checkInRepository = checkInRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInResponse> Handle(CreateCheckInByMemberIdCommand command, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetTrackedByIdAsync(command.MemberId, cancellationToken)
            ?? throw new NotFoundException($"Member with id '{command.MemberId}' was not found.");

        return await CreateCheckInAsync(member, CheckInRequestMappings.NormalizeOptionalValue(command.Request.Note), command.Principal, cancellationToken);
    }

    private async Task<CheckInResponse> CreateCheckInAsync(
        Member member,
        string? note,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var admin = await ResolveAdminAsync(principal, cancellationToken);

        if (!member.IsActive)
        {
            throw new BadRequestException($"Member '{member.Id}' is inactive.");
        }

        var activePayment = await FindTrackedActivePaymentForMemberAsync(member.Id, cancellationToken);
        if (activePayment is null)
        {
            throw new BadRequestException("Member does not have an active membership.");
        }

        var utcNow = DateTime.UtcNow;
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (activePayment.TotalVisits.HasValue && activePayment.UsedVisits.HasValue)
            {
                activePayment.UsedVisits += 1;
            }

            var checkIn = new CheckIn
            {
                Member = member,
                MembershipPayment = activePayment,
                CheckedInByUser = admin,
                CheckedInAt = utcNow,
                WasMembershipValid = true,
                Note = note,
                CreatedAt = utcNow
            };

            _checkInRepository.Add(checkIn);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return CheckInRequestMappings.MapResponse(checkIn);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private async Task<User> ResolveAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetRequiredUserId();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Current user could not be resolved.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        if (user.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Only admins can create check-ins.");
        }

        return user;
    }

    private async Task<MembershipPayment?> FindTrackedActivePaymentForMemberAsync(int memberId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var payments = await _membershipPaymentRepository.GetForMemberTrackedWithDetailsAsync(memberId, cancellationToken);
        return MembershipPaymentRules.SelectPreferredActivePayment(payments, today);
    }
}

public sealed record CreateCheckInByMembershipCodeCommand(string MembershipCode, CreateCheckInByCodeRequest Request, ClaimsPrincipal Principal) : IRequest<CheckInResponse>;

public sealed class CreateCheckInByMembershipCodeCommandHandler : IRequestHandler<CreateCheckInByMembershipCodeCommand, CheckInResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCheckInByMembershipCodeCommandHandler(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IMembershipPaymentRepository membershipPaymentRepository,
        ICheckInRepository checkInRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
        _checkInRepository = checkInRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInResponse> Handle(CreateCheckInByMembershipCodeCommand command, CancellationToken cancellationToken)
    {
        var normalizedCode = command.MembershipCode.Trim().ToUpperInvariant();
        var member = await _memberRepository.GetTrackedByCodeAsync(normalizedCode, cancellationToken)
            ?? throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.");

        return await CreateCheckInAsync(member, CheckInRequestMappings.NormalizeOptionalValue(command.Request.Note), command.Principal, cancellationToken);
    }

    private async Task<CheckInResponse> CreateCheckInAsync(
        Member member,
        string? note,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var admin = await ResolveAdminAsync(principal, cancellationToken);

        if (!member.IsActive)
        {
            throw new BadRequestException($"Member '{member.Id}' is inactive.");
        }

        var activePayment = await FindTrackedActivePaymentForMemberAsync(member.Id, cancellationToken);
        if (activePayment is null)
        {
            throw new BadRequestException("Member does not have an active membership.");
        }

        var utcNow = DateTime.UtcNow;
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (activePayment.TotalVisits.HasValue && activePayment.UsedVisits.HasValue)
            {
                activePayment.UsedVisits += 1;
            }

            var checkIn = new CheckIn
            {
                Member = member,
                MembershipPayment = activePayment,
                CheckedInByUser = admin,
                CheckedInAt = utcNow,
                WasMembershipValid = true,
                Note = note,
                CreatedAt = utcNow
            };

            _checkInRepository.Add(checkIn);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return CheckInRequestMappings.MapResponse(checkIn);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private async Task<User> ResolveAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetRequiredUserId();
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Current user could not be resolved.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        if (user.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Only admins can create check-ins.");
        }

        return user;
    }

    private async Task<MembershipPayment?> FindTrackedActivePaymentForMemberAsync(int memberId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var payments = await _membershipPaymentRepository.GetForMemberTrackedWithDetailsAsync(memberId, cancellationToken);
        return MembershipPaymentRules.SelectPreferredActivePayment(payments, today);
    }
}

file static class CheckInRequestMappings
{
    public static CheckInResponse MapResponse(CheckIn checkIn) =>
        new()
        {
            Id = checkIn.Id,
            MemberId = checkIn.MemberId,
            MemberFullName = $"{checkIn.Member.FirstName} {checkIn.Member.LastName}".Trim(),
            MembershipPaymentId = checkIn.MembershipPaymentId,
            PlanName = checkIn.MembershipPayment.MembershipPlan.Name,
            CheckedInAt = checkIn.CheckedInAt,
            WasMembershipValid = checkIn.WasMembershipValid,
            RemainingVisits = MembershipPaymentRules.CalculateRemainingVisits(checkIn.MembershipPayment),
            Message = BuildMessage(checkIn.MembershipPayment)
        };

    public static string BuildMessage(MembershipPayment payment) =>
        payment.MembershipPlan.PlanType switch
        {
            MembershipPlanType.TimeBased => "Check-in successful with an active time-based membership.",
            MembershipPlanType.VisitBased => $"Check-in successful. Remaining visits: {MembershipPaymentRules.CalculateRemainingVisits(payment)}.",
            MembershipPlanType.Combined => $"Check-in successful. Remaining visits: {MembershipPaymentRules.CalculateRemainingVisits(payment)}.",
            _ => "Check-in successful."
        };

    public static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
