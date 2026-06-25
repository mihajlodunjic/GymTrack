using GymTrack.Application.Students;
using GymTrack.DTOs.Student;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GymTrack.Controllers;

[ApiController]
[Route("api/students")]
public sealed class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudentResponse>> Create([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new CreateStudentCommand(request), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
