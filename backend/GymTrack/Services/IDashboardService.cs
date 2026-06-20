using GymTrack.DTOs.Dashboard;

namespace GymTrack.Services;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpiringMembershipResponse>> GetExpiringMembershipsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemNotificationResponse>> GetRecentNotificationsAsync(CancellationToken cancellationToken = default);
}
