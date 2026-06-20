using GymTrack.DTOs.CheckIn;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/check-ins")]
public sealed class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CheckInResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckInResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _checkInService.GetAllCheckInsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("member/{memberId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<CheckInResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CheckInResponse>>> GetForMember(int memberId, CancellationToken cancellationToken)
    {
        var response = await _checkInService.GetCheckInsForMemberAsync(memberId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("member/{memberId:int}")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckInResponse>> CreateByMemberId(
        int memberId,
        [FromBody] CreateCheckInByMemberIdRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await _checkInService.CheckInByMemberIdAsync(memberId, request ?? new CreateCheckInByMemberIdRequest(), User, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("code/{membershipCode}")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckInResponse>> CreateByCode(
        string membershipCode,
        [FromBody] CreateCheckInByCodeRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await _checkInService.CheckInByMembershipCodeAsync(membershipCode, request ?? new CreateCheckInByCodeRequest(), User, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<CheckInResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<CheckInResponse>>> GetMe(CancellationToken cancellationToken)
    {
        var response = await _checkInService.GetCurrentMemberCheckInsAsync(User, cancellationToken);
        return Ok(response);
    }
}
