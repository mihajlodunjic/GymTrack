using GymTrack.Application.MembershipPlans;
using GymTrack.DTOs.MembershipPlan;
using GymTrack.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/membership-plans")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class MembershipPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembershipPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MembershipPlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MembershipPlanResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetAllMembershipPlansQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPlanResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetMembershipPlanByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("time-based")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembershipPlanResponse>> CreateTimeBased([FromBody] CreateTimeBasedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new CreateTimeBasedPlanCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("visit-based")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembershipPlanResponse>> CreateVisitBased([FromBody] CreateVisitBasedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new CreateVisitBasedPlanCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPost("combined")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembershipPlanResponse>> CreateCombined([FromBody] CreateCombinedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new CreateCombinedPlanCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("time-based/{id:int}")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPlanResponse>> UpdateTimeBased(int id, [FromBody] UpdateTimeBasedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateTimeBasedPlanCommand(id, request), cancellationToken);
        return Ok(response);
    }

    [HttpPut("visit-based/{id:int}")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPlanResponse>> UpdateVisitBased(int id, [FromBody] UpdateVisitBasedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateVisitBasedPlanCommand(id, request), cancellationToken);
        return Ok(response);
    }

    [HttpPut("combined/{id:int}")]
    [ProducesResponseType(typeof(MembershipPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPlanResponse>> UpdateCombined(int id, [FromBody] UpdateCombinedPlanRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new UpdateCombinedPlanCommand(id, request), cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateMembershipPlanCommand(id), cancellationToken);
        return NoContent();
    }
}
