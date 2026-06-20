using System.Security.Claims;
using GymTrack.Common;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.CheckIn;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GymTrack.Services;

public sealed class CheckInService : ICheckInService
{
    private readonly AppDbContext _dbContext;

    public CheckInService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CheckInResponse> CheckInByMemberIdAsync(
        int memberId,
        CreateCheckInByMemberIdRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var member = await FindTrackedMemberByIdAsync(memberId, cancellationToken);
        return await CreateCheckInAsync(member, NormalizeOptionalValue(request.Note), principal, cancellationToken);
    }

    public async Task<CheckInResponse> CheckInByMembershipCodeAsync(
        string membershipCode,
        CreateCheckInByCodeRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var member = await FindTrackedMemberByCodeAsync(membershipCode, cancellationToken);
        return await CreateCheckInAsync(member, NormalizeOptionalValue(request.Note), principal, cancellationToken);
    }

    public async Task<IReadOnlyList<CheckInResponse>> GetAllCheckInsAsync(CancellationToken cancellationToken = default)
    {
        var checkIns = await CreateCheckInQuery()
            .OrderByDescending(checkIn => checkIn.CheckedInAt)
            .ThenByDescending(checkIn => checkIn.Id)
            .ToListAsync(cancellationToken);

        return checkIns.Select(MapResponse).ToArray();
    }

    public async Task<IReadOnlyList<CheckInResponse>> GetCheckInsForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberExistsAsync(memberId, cancellationToken);

        var checkIns = await CreateCheckInQuery()
            .Where(checkIn => checkIn.MemberId == memberId)
            .OrderByDescending(checkIn => checkIn.CheckedInAt)
            .ThenByDescending(checkIn => checkIn.Id)
            .ToListAsync(cancellationToken);

        return checkIns.Select(MapResponse).ToArray();
    }

    public async Task<IReadOnlyList<CheckInResponse>> GetCurrentMemberCheckInsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var memberId = principal.GetRequiredMemberId();
        return await GetCheckInsForMemberAsync(memberId, cancellationToken);
    }

    private IQueryable<CheckIn> CreateCheckInQuery() =>
        _dbContext.CheckIns
            .AsNoTracking()
            .Include(checkIn => checkIn.Member)
            .Include(checkIn => checkIn.MembershipPayment)
            .ThenInclude(payment => payment.MembershipPlan);

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
        IDbContextTransaction? transaction = null;

        if (_dbContext.Database.IsRelational())
        {
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

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

            _dbContext.CheckIns.Add(checkIn);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return MapResponse(checkIn);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<User> ResolveAdminAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetRequiredUserId();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

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

    private async Task<Member> FindTrackedMemberByCodeAsync(string membershipCode, CancellationToken cancellationToken)
    {
        var normalizedCode = membershipCode.Trim().ToUpperInvariant();

        var member = await _dbContext.Members
            .SingleOrDefaultAsync(entity => entity.MembershipCode == normalizedCode, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.");
        }

        return member;
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

    private async Task<MembershipPayment?> FindTrackedActivePaymentForMemberAsync(int memberId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var payments = await _dbContext.MembershipPayments
            .Include(payment => payment.Member)
            .Include(payment => payment.MembershipPlan)
            .Where(payment => payment.MemberId == memberId)
            .ToListAsync(cancellationToken);

        return MembershipPaymentRules.SelectPreferredActivePayment(payments, today);
    }

    private static CheckInResponse MapResponse(CheckIn checkIn) =>
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

    private static string BuildMessage(MembershipPayment payment) =>
        payment.MembershipPlan.PlanType switch
        {
            MembershipPlanType.TimeBased => "Check-in successful with an active time-based membership.",
            MembershipPlanType.VisitBased => $"Check-in successful. Remaining visits: {MembershipPaymentRules.CalculateRemainingVisits(payment)}.",
            MembershipPlanType.Combined => $"Check-in successful. Remaining visits: {MembershipPaymentRules.CalculateRemainingVisits(payment)}.",
            _ => "Check-in successful."
        };

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
