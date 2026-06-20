using GymTrack.Data;
using GymTrack.DTOs.Dashboard;
using GymTrack.Entities;
using GymTrack.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class HangfireJobService : IHangfireJobService
{
    private readonly AppDbContext _dbContext;
    private readonly IDashboardService _dashboardService;

    public HangfireJobService(AppDbContext dbContext, IDashboardService dashboardService)
    {
        _dbContext = dbContext;
        _dashboardService = dashboardService;
    }

    public async Task CheckExpiringMembershipsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var title = $"Expiring memberships {today:yyyy-MM-dd}";

        if (await NotificationExistsAsync(title))
        {
            return;
        }

        var expiringMemberships = await _dashboardService.GetExpiringMembershipsAsync();
        var count = expiringMemberships.Count;

        var message = count == 0
            ? "No memberships are expiring in the next 7 days."
            : BuildExpiringMembershipsMessage(expiringMemberships);

        _dbContext.SystemNotifications.Add(new SystemNotification
        {
            Title = title,
            Message = message,
            Type = count > 0 ? SystemNotificationType.Warning : SystemNotificationType.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task CreateDailyAdminReportAsync()
    {
        var today = DateTime.UtcNow.Date;
        var title = $"Daily admin report {today:yyyy-MM-dd}";

        if (await NotificationExistsAsync(title))
        {
            return;
        }

        var stats = await _dashboardService.GetDashboardStatsAsync();
        var message = BuildDailyReportMessage(stats);

        _dbContext.SystemNotifications.Add(new SystemNotification
        {
            Title = title,
            Message = message,
            Type = SystemNotificationType.Report,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
    }

    private Task<bool> NotificationExistsAsync(string title) =>
        _dbContext.SystemNotifications
            .AsNoTracking()
            .AnyAsync(notification => notification.Title == title);

    private static string BuildExpiringMembershipsMessage(IReadOnlyList<ExpiringMembershipResponse> memberships)
    {
        var preview = memberships
            .Take(3)
            .Select(membership => $"{membership.MemberFullName} ({membership.MembershipCode}) - {membership.ValidUntil:yyyy-MM-dd}");

        return $"{memberships.Count} memberships are expiring in the next 7 days. {string.Join("; ", preview)}";
    }

    private static string BuildDailyReportMessage(DashboardStatsResponse stats) =>
        $"Today check-ins: {stats.TodayCheckIns}. Current month payments: {stats.CurrentMonthPayments}. Current month revenue: {stats.CurrentMonthRevenue:0.00}. Active memberships: {stats.ActiveMemberships}.";
}
