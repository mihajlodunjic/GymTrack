using GymTrack.Common;
using GymTrack.Data;
using GymTrack.DTOs.Dashboard;
using GymTrack.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var nextSevenDays = today.AddDays(7);

        var totalMembers = await _dbContext.Members.AsNoTracking().CountAsync(cancellationToken);
        var activeMembers = await _dbContext.Members.AsNoTracking().CountAsync(member => member.IsActive, cancellationToken);
        var inactiveMembers = totalMembers - activeMembers;

        var payments = await _dbContext.MembershipPayments
            .AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.MembershipPlan)
            .ToListAsync(cancellationToken);

        var activeMemberships = payments.Count(payment => MembershipPaymentRules.IsActive(payment, today));
        var expiredMemberships = payments.Count(payment => MembershipPaymentRules.IsExpired(payment, today));
        var currentMonthPayments = payments.Count(payment => payment.PaidAt >= monthStart && payment.PaidAt < monthEnd);
        var currentMonthRevenue = payments
            .Where(payment => payment.PaidAt >= monthStart && payment.PaidAt < monthEnd)
            .Sum(payment => payment.Amount);

        var expiringInNextSevenDays = payments.Count(payment =>
            MembershipPaymentRules.IsActive(payment, today) &&
            payment.ValidUntil.HasValue &&
            payment.ValidUntil.Value.Date >= today &&
            payment.ValidUntil.Value.Date <= nextSevenDays);

        var todayCheckIns = await _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(checkIn => checkIn.CheckedInAt >= today && checkIn.CheckedInAt < today.AddDays(1), cancellationToken);

        return new DashboardStatsResponse
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            InactiveMembers = inactiveMembers,
            ActiveMemberships = activeMemberships,
            ExpiredMemberships = expiredMemberships,
            TodayCheckIns = todayCheckIns,
            CurrentMonthPayments = currentMonthPayments,
            CurrentMonthRevenue = currentMonthRevenue,
            ExpiringInNextSevenDays = expiringInNextSevenDays
        };
    }

    public async Task<IReadOnlyList<ExpiringMembershipResponse>> GetExpiringMembershipsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var nextSevenDays = today.AddDays(7);

        var payments = await _dbContext.MembershipPayments
            .AsNoTracking()
            .Include(payment => payment.Member)
            .Include(payment => payment.MembershipPlan)
            .Where(payment => payment.ValidUntil.HasValue)
            .ToListAsync(cancellationToken);

        return payments
            .Where(payment =>
                MembershipPaymentRules.IsActive(payment, today) &&
                payment.ValidUntil!.Value.Date >= today &&
                payment.ValidUntil.Value.Date <= nextSevenDays)
            .OrderBy(payment => payment.ValidUntil)
            .ThenBy(payment => payment.Member.FirstName)
            .ThenBy(payment => payment.Member.LastName)
            .Select(payment => MapExpiringMembership(payment, today))
            .ToArray();
    }

    public async Task<IReadOnlyList<SystemNotificationResponse>> GetRecentNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.SystemNotifications
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        return notifications
            .Select(MapNotification)
            .ToArray();
    }

    private static ExpiringMembershipResponse MapExpiringMembership(MembershipPayment payment, DateTime today) =>
        new()
        {
            MemberId = payment.MemberId,
            MemberFullName = $"{payment.Member.FirstName} {payment.Member.LastName}".Trim(),
            MembershipCode = payment.Member.MembershipCode,
            MembershipPaymentId = payment.Id,
            PlanName = payment.MembershipPlan.Name,
            PlanType = payment.MembershipPlan.PlanType,
            ValidUntil = payment.ValidUntil!.Value,
            DaysUntilExpiration = (payment.ValidUntil.Value.Date - today).Days,
            RemainingVisits = MembershipPaymentRules.CalculateRemainingVisits(payment)
        };

    private static SystemNotificationResponse MapNotification(SystemNotification notification) =>
        new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
}
