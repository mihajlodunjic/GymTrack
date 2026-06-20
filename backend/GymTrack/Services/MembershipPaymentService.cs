using System.Security.Claims;
using GymTrack.Common;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.MembershipPayment;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Security;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class MembershipPaymentService : IMembershipPaymentService
{
    private readonly AppDbContext _dbContext;

    public MembershipPaymentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> GetAllPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var payments = await CreatePaymentQuery()
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .ToListAsync(cancellationToken);

        return payments.Select(MapPaymentResponse).ToArray();
    }

    public async Task<MembershipPaymentResponse> GetPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await FindPaymentByIdAsync(paymentId, cancellationToken);
        return MapPaymentResponse(payment);
    }

    public async Task<MembershipPaymentResponse> CreatePaymentAsync(
        CreateMembershipPaymentRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (request.ValidFrom == default)
        {
            throw new BadRequestException("ValidFrom is required.");
        }

        var creator = await ResolveCreatorAsync(principal, cancellationToken);
        var member = await FindTrackedMemberByIdAsync(request.MemberId, cancellationToken);
        var plan = await FindTrackedPlanByIdAsync(request.MembershipPlanId, cancellationToken);

        if (!member.IsActive)
        {
            throw new BadRequestException($"Member '{member.Id}' is inactive.");
        }

        if (!plan.IsActive)
        {
            throw new BadRequestException($"Membership plan '{plan.Id}' is inactive.");
        }

        var validFrom = request.ValidFrom.Date;
        var payment = new MembershipPayment
        {
            Member = member,
            MembershipPlan = plan,
            CreatedByUser = creator,
            Amount = plan.Price,
            PaidAt = DateTime.UtcNow,
            ValidFrom = validFrom,
            ValidUntil = CalculateValidUntil(plan, validFrom),
            TotalVisits = plan.IncludedVisits,
            UsedVisits = plan.IncludedVisits.HasValue ? 0 : null,
            Note = NormalizeOptionalValue(request.Note)
        };

        _dbContext.MembershipPayments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapPaymentResponse(payment);
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> GetPaymentsForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberExistsAsync(memberId, cancellationToken);

        var payments = await CreatePaymentQuery()
            .Where(payment => payment.MemberId == memberId)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .ToListAsync(cancellationToken);

        return payments.Select(MapPaymentResponse).ToArray();
    }

    public async Task<IReadOnlyList<MembershipPaymentResponse>> GetCurrentMemberPaymentsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var memberId = principal.GetRequiredMemberId();
        return await GetPaymentsForMemberAsync(memberId, cancellationToken);
    }

    public async Task<MembershipPaymentResponse?> GetActiveMembershipForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberExistsAsync(memberId, cancellationToken);

        var activePayment = await FindActivePaymentEntityForMemberAsync(memberId, cancellationToken);
        return activePayment is null ? null : MapPaymentResponse(activePayment);
    }

    public async Task<MembershipStatusResponse> GetMembershipStatusForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await FindMemberByIdAsync(memberId, cancellationToken);
        var activePayment = await FindActivePaymentEntityForMemberAsync(memberId, cancellationToken);

        return MapStatusResponse(member, activePayment);
    }

    public async Task<MembershipStatusResponse> GetCurrentMemberStatusAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var memberId = principal.GetRequiredMemberId();
        return await GetMembershipStatusForMemberAsync(memberId, cancellationToken);
    }

    public async Task<MembershipStatusResponse> GetMembershipStatusByCodeAsync(string membershipCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeMembershipCode(membershipCode);

        var member = await _dbContext.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.MembershipCode == normalizedCode, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.");
        }

        var activePayment = await FindActivePaymentEntityForMemberAsync(member.Id, cancellationToken);
        return MapStatusResponse(member, activePayment);
    }

    private IQueryable<MembershipPayment> CreatePaymentQuery() =>
        _dbContext.MembershipPayments
            .AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.MembershipPlan);

    private async Task<User> ResolveCreatorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var creatorUserId = principal.GetRequiredUserId();
        var creator = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == creatorUserId, cancellationToken);

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

    private async Task<MembershipPayment> FindPaymentByIdAsync(int paymentId, CancellationToken cancellationToken)
    {
        var payment = await CreatePaymentQuery()
            .SingleOrDefaultAsync(entity => entity.Id == paymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Membership payment with id '{paymentId}' was not found.");
        }

        return payment;
    }

    private async Task<Member> FindMemberByIdAsync(int memberId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        return member;
    }

    private async Task<Member> FindTrackedMemberByIdAsync(int memberId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.Members
            .SingleOrDefaultAsync(entity => entity.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        return member;
    }

    private async Task<MembershipPlan> FindTrackedPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.MembershipPlans
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        if (plan is null)
        {
            throw new NotFoundException($"Membership plan with id '{planId}' was not found.");
        }

        return plan;
    }

    private async Task EnsureMemberExistsAsync(int memberId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Members
            .AsNoTracking()
            .AnyAsync(member => member.Id == memberId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }
    }

    private async Task<MembershipPayment?> FindActivePaymentEntityForMemberAsync(int memberId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var payments = await CreatePaymentQuery()
            .Where(payment => payment.MemberId == memberId)
            .ToListAsync(cancellationToken);

        return MembershipPaymentRules.SelectPreferredActivePayment(payments, today);
    }

    private static MembershipPaymentResponse MapPaymentResponse(MembershipPayment payment) =>
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

    private static MembershipStatusResponse MapStatusResponse(Member member, MembershipPayment? activePayment)
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

    private static DateTime? CalculateValidUntil(MembershipPlan plan, DateTime validFrom) =>
        plan.DurationInDays.HasValue
            ? validFrom.AddDays(plan.DurationInDays.Value)
            : null;

    private static string BuildStatusMessage(MembershipPayment payment) =>
        payment.MembershipPlan.PlanType switch
        {
            MembershipPlanType.TimeBased => $"Active time-based membership until {payment.ValidUntil:yyyy-MM-dd}.",
            MembershipPlanType.VisitBased => $"Active visit-based membership with {MembershipPaymentRules.CalculateRemainingVisits(payment)} remaining visits.",
            MembershipPlanType.Combined => $"Active combined membership until {payment.ValidUntil:yyyy-MM-dd} with {MembershipPaymentRules.CalculateRemainingVisits(payment)} remaining visits.",
            _ => "Active membership."
        };

    private static string GetFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();

    private static string NormalizeMembershipCode(string membershipCode) =>
        membershipCode.Trim().ToUpperInvariant();

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
