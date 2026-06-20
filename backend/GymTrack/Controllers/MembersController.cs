using GymTrack.DTOs.Member;
using GymTrack.DTOs.MembershipPayment;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/members")]
public sealed class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly IMembershipPaymentService _membershipPaymentService;

    public MembersController(IMemberService memberService, IMembershipPaymentService membershipPaymentService)
    {
        _memberService = memberService;
        _membershipPaymentService = membershipPaymentService;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var response = await _memberService.GetAllMembersAsync(search, isActive, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailsResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _memberService.GetMemberByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDetailsResponse>> Create([FromBody] CreateMemberRequest request, CancellationToken cancellationToken)
    {
        var response = await _memberService.CreateMemberAsync(request, cancellationToken);
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
        var response = await _memberService.UpdateMemberAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _memberService.DeactivateMemberAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("by-code/{membershipCode}")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailsResponse>> GetByCode(string membershipCode, CancellationToken cancellationToken)
    {
        var response = await _memberService.GetMemberByCodeAsync(membershipCode, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("by-code/{membershipCode}/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipStatusResponse>> GetStatusByCode(string membershipCode, CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetMembershipStatusByCodeAsync(membershipCode, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipStatusResponse>> GetStatus(int id, CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetMembershipStatusForMemberAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MemberDetailsResponse>> GetMe(CancellationToken cancellationToken)
    {
        var response = await _memberService.GetCurrentMemberProfileAsync(User, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me/status")]
    [ProducesResponseType(typeof(MembershipStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MembershipStatusResponse>> GetMyStatus(CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetCurrentMemberStatusAsync(User, cancellationToken);
        return Ok(response);
    }
}
