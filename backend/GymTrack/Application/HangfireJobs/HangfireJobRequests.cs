using GymTrack.Common;
using GymTrack.DTOs.Dashboard;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using MediatR;

namespace GymTrack.Application.HangfireJobs;

public sealed record CheckExpiringMembershipsJobCommand : IRequest;

public sealed class CheckExpiringMembershipsJobCommandHandler : IRequestHandler<CheckExpiringMembershipsJobCommand>
{
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly ISystemNotificationRepository _systemNotificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CheckExpiringMembershipsJobCommandHandler(
        IMembershipPaymentRepository membershipPaymentRepository,
        ISystemNotificationRepository systemNotificationRepository,
        IUnitOfWork unitOfWork)
    {
        _membershipPaymentRepository = membershipPaymentRepository;
        _systemNotificationRepository = systemNotificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CheckExpiringMembershipsJobCommand command, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var title = $"Expiring memberships {today:yyyy-MM-dd}";

        if (await _systemNotificationRepository.ExistsByTitleAsync(title, cancellationToken))
        {
            return;
        }

        var nextSevenDays = today.AddDays(7);
        var payments = await _membershipPaymentRepository.GetAllWithDetailsAsync(cancellationToken);
        var expiringMemberships = payments
            .Where(payment =>
                MembershipPaymentRules.IsActive(payment, today) &&
                payment.ValidUntil.HasValue &&
                payment.ValidUntil.Value.Date >= today &&
                payment.ValidUntil.Value.Date <= nextSevenDays)
            .OrderBy(payment => payment.ValidUntil)
            .ThenBy(payment => payment.Member.FirstName)
            .ThenBy(payment => payment.Member.LastName)
            .Select(payment => new ExpiringMembershipResponse
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
            })
            .ToArray();

        var count = expiringMemberships.Length;
        var message = count == 0
            ? "No memberships are expiring in the next 7 days."
            : HangfireJobRequestMappings.BuildExpiringMembershipsMessage(expiringMemberships);

        _systemNotificationRepository.Add(new SystemNotification
        {
            Title = title,
            Message = message,
            Type = count > 0 ? SystemNotificationType.Warning : SystemNotificationType.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record CreateDailyAdminReportJobCommand : IRequest;

public sealed class CreateDailyAdminReportJobCommandHandler : IRequestHandler<CreateDailyAdminReportJobCommand>
{
    private readonly IMemberRepository _memberRepository;
    private readonly IMembershipPaymentRepository _membershipPaymentRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly ISystemNotificationRepository _systemNotificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDailyAdminReportJobCommandHandler(
        IMemberRepository memberRepository,
        IMembershipPaymentRepository membershipPaymentRepository,
        ICheckInRepository checkInRepository,
        ISystemNotificationRepository systemNotificationRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _membershipPaymentRepository = membershipPaymentRepository;
        _checkInRepository = checkInRepository;
        _systemNotificationRepository = systemNotificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateDailyAdminReportJobCommand command, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var title = $"Daily admin report {today:yyyy-MM-dd}";

        if (await _systemNotificationRepository.ExistsByTitleAsync(title, cancellationToken))
        {
            return;
        }

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var nextSevenDays = today.AddDays(7);

        var totalMembers = await _memberRepository.CountAsync(null, cancellationToken);
        var activeMembers = await _memberRepository.CountAsync(true, cancellationToken);
        var payments = await _membershipPaymentRepository.GetAllWithDetailsAsync(cancellationToken);
        var stats = new DashboardStatsResponse
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            InactiveMembers = totalMembers - activeMembers,
            ActiveMemberships = payments.Count(payment => MembershipPaymentRules.IsActive(payment, today)),
            ExpiredMemberships = payments.Count(payment => MembershipPaymentRules.IsExpired(payment, today)),
            TodayCheckIns = await _checkInRepository.CountForDateAsync(today, cancellationToken),
            CurrentMonthPayments = payments.Count(payment => payment.PaidAt >= monthStart && payment.PaidAt < monthEnd),
            CurrentMonthRevenue = payments
                .Where(payment => payment.PaidAt >= monthStart && payment.PaidAt < monthEnd)
                .Sum(payment => payment.Amount),
            ExpiringInNextSevenDays = payments.Count(payment =>
                MembershipPaymentRules.IsActive(payment, today) &&
                payment.ValidUntil.HasValue &&
                payment.ValidUntil.Value.Date >= today &&
                payment.ValidUntil.Value.Date <= nextSevenDays)
        };

        _systemNotificationRepository.Add(new SystemNotification
        {
            Title = title,
            Message = HangfireJobRequestMappings.BuildDailyReportMessage(stats),
            Type = SystemNotificationType.Report,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

file static class HangfireJobRequestMappings
{
    public static string BuildExpiringMembershipsMessage(IReadOnlyList<ExpiringMembershipResponse> memberships)
    {
        var preview = memberships
            .Take(3)
            .Select(membership => $"{membership.MemberFullName} ({membership.MembershipCode}) - {membership.ValidUntil:yyyy-MM-dd}");

        return $"{memberships.Count} memberships are expiring in the next 7 days. {string.Join("; ", preview)}";
    }

    public static string BuildDailyReportMessage(DashboardStatsResponse stats) =>
        $"Today check-ins: {stats.TodayCheckIns}. Current month payments: {stats.CurrentMonthPayments}. Current month revenue: {stats.CurrentMonthRevenue:0.00}. Active memberships: {stats.ActiveMemberships}.";
}
