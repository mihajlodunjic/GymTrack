using GymTrack.DTOs.MembershipPayment;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/membership-payments")]
public sealed class MembershipPaymentsController : ControllerBase
{
    private readonly IMembershipPaymentService _membershipPaymentService;

    public MembershipPaymentsController(IMembershipPaymentService membershipPaymentService)
    {
        _membershipPaymentService = membershipPaymentService;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipPaymentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MembershipPaymentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetAllPaymentsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MembershipPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPaymentResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetPaymentByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ProducesResponseType(typeof(MembershipPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPaymentResponse>> Create(
        [FromBody] CreateMembershipPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.CreatePaymentAsync(request, User, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("member/{memberId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MembershipPaymentResponse>>> GetForMember(int memberId, CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetPaymentsForMemberAsync(memberId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = nameof(UserRole.Member))]
    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MembershipPaymentResponse>>> GetMe(CancellationToken cancellationToken)
    {
        var response = await _membershipPaymentService.GetCurrentMemberPaymentsAsync(User, cancellationToken);
        return Ok(response);
    }
}
