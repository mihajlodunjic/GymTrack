using System.Security.Claims;
using GymTrack.Common;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.MembershipPayment;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using MediatR;

namespace GymTrack.Application.MembershipPayments;

public sealed record GetAllMembershipPaymentsQuery : IRequest<IReadOnlyList<MembershipPaymentResponse>>;

public sealed class GetAllMembershipPaymentsQueryHandler : IRequestHandler<GetAllMembershipPaymentsQuery, IReadOnlyList<MembershipPaymentResponse>>
{
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetAllMembershipPaymentsQueryHandler(IMembershipPaymentRepository membershipPaymentRepository)
    {
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> Handle(GetAllMembershipPaymentsQuery query, CancellationToken cancellationToken)
    {
        var payments = await _membershipPaymentRepository.GetAllWithDetailsAsync(cancellationToken);
        return payments
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .Select(MembershipPaymentRequestMappings.MapPaymentResponse)
            .ToArray();
    }
}

public sealed record GetMembershipPaymentByIdQuery(int PaymentId) : IRequest<MembershipPaymentResponse>;

public sealed class GetMembershipPaymentByIdQueryHandler : IRequestHandler<GetMembershipPaymentByIdQuery, MembershipPaymentResponse>
{
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetMembershipPaymentByIdQueryHandler(IMembershipPaymentRepository membershipPaymentRepository)
    {
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<MembershipPaymentResponse> Handle(GetMembershipPaymentByIdQuery query, CancellationToken cancellationToken)
    {
        var payment = await _membershipPaymentRepository.GetByIdWithDetailsAsync(query.PaymentId, cancellationToken);
        return payment is null
            ? throw new NotFoundException($"Membership payment with id '{query.PaymentId}' was not found.")
            : MembershipPaymentRequestMappings.MapPaymentResponse(payment);
    }
}

public sealed record CreateMembershipPaymentCommand(CreateMembershipPaymentRequest Request, ClaimsPrincipal Principal) : IRequest<MembershipPaymentResponse>;

public sealed class CreateMembershipPaymentCommandHandler : IRequestHandler<CreateMembershipPaymentCommand, MembershipPaymentResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMembershipPaymentCommandHandler(
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IMembershipPlanRepository membershipPlanRepository,
        IMembershipPaymentRepository membershipPaymentRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _membershipPlanRepository = membershipPlanRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPaymentResponse> Handle(CreateMembershipPaymentCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.ValidFrom == default)
        {
            throw new BadRequestException("ValidFrom is required.");
        }

        var creator = await ResolveCreatorAsync(command.Principal, cancellationToken);
        var member = await _memberRepository.GetTrackedByIdAsync(command.Request.MemberId, cancellationToken)
            ?? throw new NotFoundException($"Member with id '{command.Request.MemberId}' was not found.");
        var plan = await _membershipPlanRepository.GetTrackedByIdAsync(command.Request.MembershipPlanId, cancellationToken)
            ?? throw new NotFoundException($"Membership plan with id '{command.Request.MembershipPlanId}' was not found.");

        if (!member.IsActive)
        {
            throw new BadRequestException($"Member '{member.Id}' is inactive.");
        }

        if (!plan.IsActive)
        {
            throw new BadRequestException($"Membership plan '{plan.Id}' is inactive.");
        }

        var validFrom = command.Request.ValidFrom.Date;
        var payment = new MembershipPayment
        {
            Member = member,
            MembershipPlan = plan,
            CreatedByUser = creator,
            Amount = plan.Price,
            PaidAt = DateTime.UtcNow,
            ValidFrom = validFrom,
            ValidUntil = MembershipPaymentRequestMappings.CalculateValidUntil(plan, validFrom),
            TotalVisits = plan.IncludedVisits,
            UsedVisits = plan.IncludedVisits.HasValue ? 0 : null,
            Note = MembershipPaymentRequestMappings.NormalizeOptionalValue(command.Request.Note)
        };

        _membershipPaymentRepository.Add(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MembershipPaymentRequestMappings.MapPaymentResponse(payment);
    }

    private async Task<User> ResolveCreatorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var creatorUserId = principal.GetRequiredUserId();
        var creator = await _userRepository.GetByIdAsync(creatorUserId, cancellationToken);

        if (creator is null)
        {
            throw new UnauthorizedException("Current user could not be resolved.");
        }

        if (!creator.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        if (creator.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Only admins can create membership payments.");
        }

        return creator;
    }
}

public sealed record GetPaymentsForMemberQuery(int MemberId) : IRequest<IReadOnlyList<MembershipPaymentResponse>>;

public sealed class GetPaymentsForMemberQueryHandler : IRequestHandler<GetPaymentsForMemberQuery, IReadOnlyList<MembershipPaymentResponse>>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetPaymentsForMemberQueryHandler(IMemberRepository memberRepository, IMembershipPaymentRepository membershipPaymentRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> Handle(GetPaymentsForMemberQuery query, CancellationToken cancellationToken)
    {
        await EnsureMemberExistsAsync(query.MemberId, cancellationToken);

        var payments = await _membershipPaymentRepository.GetForMemberWithDetailsAsync(query.MemberId, cancellationToken);
        return payments
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .Select(MembershipPaymentRequestMappings.MapPaymentResponse)
            .ToArray();
    }

    private async Task EnsureMemberExistsAsync(int memberId, CancellationToken cancellationToken)
    {
        if (!await _memberRepository.ExistsAsync(memberId, cancellationToken))
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }
    }
}

public sealed record GetCurrentMemberPaymentsQuery(ClaimsPrincipal Principal) : IRequest<IReadOnlyList<MembershipPaymentResponse>>;

public sealed class GetCurrentMemberPaymentsQueryHandler : IRequestHandler<GetCurrentMemberPaymentsQuery, IReadOnlyList<MembershipPaymentResponse>>
{
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly IMemberRepository _memberRepository;

    public GetCurrentMemberPaymentsQueryHandler(IMembershipPaymentRepository membershipPaymentRepository, IMemberRepository memberRepository)
    {
        _membershipPaymentRepository = membershipPaymentRepository;
        _memberRepository = memberRepository;
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> Handle(GetCurrentMemberPaymentsQuery query, CancellationToken cancellationToken)
    {
        var memberId = query.Principal.GetRequiredMemberId();
        if (!await _memberRepository.ExistsAsync(memberId, cancellationToken))
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        var payments = await _membershipPaymentRepository.GetForMemberWithDetailsAsync(memberId, cancellationToken);
        return payments
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .Select(MembershipPaymentRequestMappings.MapPaymentResponse)
            .ToArray();
    }
}

public sealed record GetActiveMembershipForMemberQuery(int MemberId) : IRequest<MembershipPaymentResponse?>;

public sealed class GetActiveMembershipForMemberQueryHandler : IRequestHandler<GetActiveMembershipForMemberQuery, MembershipPaymentResponse?>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetActiveMembershipForMemberQueryHandler(IMemberRepository memberRepository, IMembershipPaymentRepository membershipPaymentRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<MembershipPaymentResponse?> Handle(GetActiveMembershipForMemberQuery query, CancellationToken cancellationToken)
    {
        if (!await _memberRepository.ExistsAsync(query.MemberId, cancellationToken))
        {
            throw new NotFoundException($"Member with id '{query.MemberId}' was not found.");
        }

        var activePayment = await MembershipPaymentRequestHelpers.FindActivePaymentEntityForMemberAsync(query.MemberId, _membershipPaymentRepository, cancellationToken);
        return activePayment is null ? null : MembershipPaymentRequestMappings.MapPaymentResponse(activePayment);
    }
}

public sealed record GetMembershipStatusForMemberQuery(int MemberId) : IRequest<MembershipStatusResponse>;

public sealed class GetMembershipStatusForMemberQueryHandler : IRequestHandler<GetMembershipStatusForMemberQuery, MembershipStatusResponse>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetMembershipStatusForMemberQueryHandler(IMemberRepository memberRepository, IMembershipPaymentRepository membershipPaymentRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<MembershipStatusResponse> Handle(GetMembershipStatusForMemberQuery query, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(query.MemberId, true, cancellationToken)
            ?? throw new NotFoundException($"Member with id '{query.MemberId}' was not found.");

        var activePayment = await MembershipPaymentRequestHelpers.FindActivePaymentEntityForMemberAsync(query.MemberId, _membershipPaymentRepository, cancellationToken);
        return MembershipPaymentRequestMappings.MapStatusResponse(member, activePayment);
    }
}

public sealed record GetCurrentMemberStatusQuery(ClaimsPrincipal Principal) : IRequest<MembershipStatusResponse>;

public sealed class GetCurrentMemberStatusQueryHandler : IRequestHandler<GetCurrentMemberStatusQuery, MembershipStatusResponse>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetCurrentMemberStatusQueryHandler(IMemberRepository memberRepository, IMembershipPaymentRepository membershipPaymentRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<MembershipStatusResponse> Handle(GetCurrentMemberStatusQuery query, CancellationToken cancellationToken)
    {
        var memberId = query.Principal.GetRequiredMemberId();
        var member = await _memberRepository.GetByIdAsync(memberId, true, cancellationToken)
            ?? throw new NotFoundException($"Member with id '{memberId}' was not found.");

        var activePayment = await MembershipPaymentRequestHelpers.FindActivePaymentEntityForMemberAsync(memberId, _membershipPaymentRepository, cancellationToken);
        return MembershipPaymentRequestMappings.MapStatusResponse(member, activePayment);
    }
}

public sealed record GetMembershipStatusByCodeQuery(string MembershipCode) : IRequest<MembershipStatusResponse>;

public sealed class GetMembershipStatusByCodeQueryHandler : IRequestHandler<GetMembershipStatusByCodeQuery, MembershipStatusResponse>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetMembershipStatusByCodeQueryHandler(IMemberRepository memberRepository, IMembershipPaymentRepository membershipPaymentRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<MembershipStatusResponse> Handle(GetMembershipStatusByCodeQuery query, CancellationToken cancellationToken)
    {
        var normalizedCode = MembershipPaymentRequestMappings.NormalizeMembershipCode(query.MembershipCode);
        var member = await _memberRepository.GetByCodeWithUserAsync(normalizedCode, true, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.");
        }

        var activePayment = await MembershipPaymentRequestHelpers.FindActivePaymentEntityForMemberAsync(member.Id, _membershipPaymentRepository, cancellationToken);
        return MembershipPaymentRequestMappings.MapStatusResponse(member, activePayment);
    }
}

file static class MembershipPaymentRequestMappings
{
    public static MembershipPaymentResponse MapPaymentResponse(MembershipPayment payment) =>
        new()
        {
            Id = payment.Id,
            MemberId = payment.MemberId,
            MemberFullName = GetFullName(payment.Member.FirstName, payment.Member.LastName),
            MembershipPlanId = payment.MembershipPlanId,
            PlanName = payment.MembershipPlan.Name,
            PlanType = payment.MembershipPlan.PlanType,
            Amount = payment.Amount,
            PaidAt = payment.PaidAt,
            ValidFrom = payment.ValidFrom,
            ValidUntil = payment.ValidUntil,
            TotalVisits = payment.TotalVisits,
            UsedVisits = payment.UsedVisits,
            RemainingVisits = MembershipPaymentRules.CalculateRemainingVisits(payment),
            Note = payment.Note
        };

    public static MembershipStatusResponse MapStatusResponse(Member member, MembershipPayment? activePayment)
    {
        if (activePayment is null)
        {
            return new MembershipStatusResponse
            {
                MemberId = member.Id,
                MemberFullName = GetFullName(member.FirstName, member.LastName),
                MembershipCode = member.MembershipCode,
                HasActiveMembership = false,
                Message = "Member does not have an active membership."
            };
        }

        return new MembershipStatusResponse
        {
            MemberId = member.Id,
            MemberFullName = GetFullName(member.FirstName, member.LastName),
            MembershipCode = member.MembershipCode,
            HasActiveMembership = true,
            ActivePaymentId = activePayment.Id,
            PlanName = activePayment.MembershipPlan.Name,
            PlanType = activePayment.MembershipPlan.PlanType,
            ValidFrom = activePayment.ValidFrom,
            ValidUntil = activePayment.ValidUntil,
            TotalVisits = activePayment.TotalVisits,
            UsedVisits = activePayment.UsedVisits,
            RemainingVisits = MembershipPaymentRules.CalculateRemainingVisits(activePayment),
            Message = BuildStatusMessage(activePayment)
        };
    }

    public static DateTime? CalculateValidUntil(MembershipPlan plan, DateTime validFrom) =>
        plan.DurationInDays.HasValue
            ? validFrom.AddDays(plan.DurationInDays.Value)
            : null;

    public static string BuildStatusMessage(MembershipPayment payment) =>
        payment.MembershipPlan.PlanType switch
        {
            MembershipPlanType.TimeBased => $"Active time-based membership until {payment.ValidUntil:yyyy-MM-dd}.",
            MembershipPlanType.VisitBased => $"Active visit-based membership with {MembershipPaymentRules.CalculateRemainingVisits(payment)} remaining visits.",
            MembershipPlanType.Combined => $"Active combined membership until {payment.ValidUntil:yyyy-MM-dd} with {MembershipPaymentRules.CalculateRemainingVisits(payment)} remaining visits.",
            _ => "Active membership."
        };

    public static string GetFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();

    public static string NormalizeMembershipCode(string membershipCode) =>
        membershipCode.Trim().ToUpperInvariant();

    public static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

file static class MembershipPaymentRequestHelpers
{
    public static async Task<MembershipPayment?> FindActivePaymentEntityForMemberAsync(
        int memberId,
        IMembershipPaymentRepository membershipPaymentRepository,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var payments = await membershipPaymentRepository.GetForMemberForActiveSelectionAsync(memberId, cancellationToken);
        return MembershipPaymentRules.SelectPreferredActivePayment(payments, today);
    }
}
