using GymTrack.Application.HangfireJobs;
using MediatR;

namespace GymTrack.Services;

public sealed class HangfireJobService : IHangfireJobService
{
    private readonly IMediator _mediator;

    public HangfireJobService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task CheckExpiringMembershipsAsync() =>
        _mediator.Send(new CheckExpiringMembershipsJobCommand());

    public Task CreateDailyAdminReportAsync() =>
        _mediator.Send(new CreateDailyAdminReportJobCommand());
}
