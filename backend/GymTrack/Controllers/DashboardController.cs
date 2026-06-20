using GymTrack.DTOs.Dashboard;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("expiring-memberships")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpiringMembershipResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpiringMembershipResponse>>> GetExpiringMemberships(CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetExpiringMembershipsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("notifications")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemNotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemNotificationResponse>>> GetNotifications(CancellationToken cancellationToken)
    {
        var response = await _dashboardService.GetRecentNotificationsAsync(cancellationToken);
        return Ok(response);
    }
}
