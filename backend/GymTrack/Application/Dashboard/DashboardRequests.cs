using GymTrack.Common;
using GymTrack.DTOs.Dashboard;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using MediatR;

namespace GymTrack.Application.Dashboard;

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsResponse>;

public sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly ICheckInRepository _checkInRepository;

    public GetDashboardStatsQueryHandler(
        IMemberRepository memberRepository,
        IMembershipPaymentRepository membershipPaymentRepository,
        ICheckInRepository checkInRepository)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
        _checkInRepository = checkInRepository;
    }

    public async Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var nextSevenDays = today.AddDays(7);

        var totalMembers = await _memberRepository.CountAsync(null, cancellationToken);
        var activeMembers = await _memberRepository.CountAsync(true, cancellationToken);
        var inactiveMembers = totalMembers - activeMembers;

        var payments = await _membershipPaymentRepository.GetAllWithDetailsAsync(cancellationToken);
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

        var todayCheckIns = await _checkInRepository.CountForDateAsync(today, cancellationToken);

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
}

public sealed record GetExpiringMembershipsQuery : IRequest<IReadOnlyList<ExpiringMembershipResponse>>;

public sealed class GetExpiringMembershipsQueryHandler : IRequestHandler<GetExpiringMembershipsQuery, IReadOnlyList<ExpiringMembershipResponse>>
{
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;

    public GetExpiringMembershipsQueryHandler(IMembershipPaymentRepository membershipPaymentRepository)
    {
        _membershipPaymentRepository = membershipPaymentRepository;
    }

    public async Task<IReadOnlyList<ExpiringMembershipResponse>> Handle(GetExpiringMembershipsQuery query, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var nextSevenDays = today.AddDays(7);
        var payments = await _membershipPaymentRepository.GetAllWithDetailsAsync(cancellationToken);

        return payments
            .Where(payment =>
                MembershipPaymentRules.IsActive(payment, today) &&
                payment.ValidUntil.HasValue &&
                payment.ValidUntil.Value.Date >= today &&
                payment.ValidUntil.Value.Date <= nextSevenDays)
            .OrderBy(payment => payment.ValidUntil)
            .ThenBy(payment => payment.Member.FirstName)
            .ThenBy(payment => payment.Member.LastName)
            .Select(payment => DashboardRequestMappings.MapExpiringMembership(payment, today))
            .ToArray();
    }
}

public sealed record GetRecentNotificationsQuery : IRequest<IReadOnlyList<SystemNotificationResponse>>;

public sealed class GetRecentNotificationsQueryHandler : IRequestHandler<GetRecentNotificationsQuery, IReadOnlyList<SystemNotificationResponse>>
{
    private readonly ISystemNotificationRepository _systemNotificationRepository;

    public GetRecentNotificationsQueryHandler(ISystemNotificationRepository systemNotificationRepository)
    {
        _systemNotificationRepository = systemNotificationRepository;
    }

    public async Task<IReadOnlyList<SystemNotificationResponse>> Handle(GetRecentNotificationsQuery query, CancellationToken cancellationToken)
    {
        var notifications = await _systemNotificationRepository.GetRecentAsync(20, cancellationToken);
        return notifications.Select(DashboardRequestMappings.MapNotification).ToArray();
    }
}

file static class DashboardRequestMappings
{
    public static ExpiringMembershipResponse MapExpiringMembership(MembershipPayment payment, DateTime today) =>
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

    public static SystemNotificationResponse MapNotification(SystemNotification notification) =>
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
