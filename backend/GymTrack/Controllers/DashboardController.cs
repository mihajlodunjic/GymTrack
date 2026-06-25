using GymTrack.Application.Dashboard;
using GymTrack.DTOs.Dashboard;
using GymTrack.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("expiring-memberships")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpiringMembershipResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpiringMembershipResponse>>> GetExpiringMemberships(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetExpiringMembershipsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("notifications")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemNotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SystemNotificationResponse>>> GetNotifications(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetRecentNotificationsQuery(), cancellationToken);
        return Ok(response);
    }
}
