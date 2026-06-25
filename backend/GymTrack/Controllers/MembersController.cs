using GymTrack.Application.Members;
using GymTrack.Application.MembershipPayments;
using GymTrack.Application.QrCodes;
using GymTrack.DTOs.Member;
using GymTrack.DTOs.MembershipPayment;
using GymTrack.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/members")]
public sealed class MembersController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetAllMembersQuery(search, isActive), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailsResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMemberByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailsResponse>> Create([FromBody] CreateMemberRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new CreateMemberCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailsResponse>> Update(int id, [FromBody] UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateMemberCommand(id, request), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateMemberCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("by-code/{membershipCode}")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailsResponse>> GetByCode(string membershipCode, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMemberByCodeQuery(membershipCode), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("by-code/{membershipCode}/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipStatusResponse>> GetStatusByCode(string membershipCode, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMembershipStatusByCodeQuery(membershipCode), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipStatusResponse>> GetStatus(int id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMembershipStatusForMemberQuery(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}/qr-code")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(int id, CancellationToken cancellationToken)
    {
        var content = await _mediator.Send(new GenerateQrCodeForMemberQuery(id), cancellationToken);
        return File(content, "image/png");
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MemberDetailsResponse>> GetMe(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetCurrentMemberProfileQuery(User), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MembershipStatusResponse>> GetMyStatus(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetCurrentMemberStatusQuery(User), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me/qr-code")]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyQrCode(CancellationToken cancellationToken)
    {
        var content = await _mediator.Send(new GenerateQrCodeForCurrentMemberQuery(User), cancellationToken);
        return File(content, "image/png");
    }
}
