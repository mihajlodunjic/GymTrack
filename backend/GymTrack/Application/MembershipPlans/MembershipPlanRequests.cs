using GymTrack.Common.Exceptions;
using GymTrack.DTOs.MembershipPlan;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using MediatR;

namespace GymTrack.Application.MembershipPlans;

public sealed record GetAllMembershipPlansQuery : IRequest<IReadOnlyList<MembershipPlanResponse>>;

public sealed class GetAllMembershipPlansQueryHandler : IRequestHandler<GetAllMembershipPlansQuery, IReadOnlyList<MembershipPlanResponse>>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;

    public GetAllMembershipPlansQueryHandler(IMembershipPlanRepository membershipPlanRepository)
    {
        _membershipPlanRepository = membershipPlanRepository;
    }

    public async Task<IReadOnlyList<MembershipPlanResponse>> Handle(GetAllMembershipPlansQuery query, CancellationToken cancellationToken)
    {
        var plans = await _membershipPlanRepository.GetAllAsync(cancellationToken);
        return plans.Select(MembershipPlanRequestMappings.MapResponse).ToArray();
    }
}

public sealed record GetMembershipPlanByIdQuery(int PlanId) : IRequest<MembershipPlanResponse>;

public sealed class GetMembershipPlanByIdQueryHandler : IRequestHandler<GetMembershipPlanByIdQuery, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;

    public GetMembershipPlanByIdQueryHandler(IMembershipPlanRepository membershipPlanRepository)
    {
        _membershipPlanRepository = membershipPlanRepository;
    }

    public async Task<MembershipPlanResponse> Handle(GetMembershipPlanByIdQuery query, CancellationToken cancellationToken)
    {
        var plan = await _membershipPlanRepository.GetByIdAsync(query.PlanId, true, cancellationToken);
        return plan is null
            ? throw new NotFoundException($"Membership plan with id '{query.PlanId}' was not found.")
            : MembershipPlanRequestMappings.MapResponse(plan);
    }
}

public sealed record CreateTimeBasedPlanCommand(CreateTimeBasedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class CreateTimeBasedPlanCommandHandler : IRequestHandler<CreateTimeBasedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTimeBasedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(CreateTimeBasedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.DurationInDays, nameof(command.Request.DurationInDays));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = new TimeBasedMembershipPlan
        {
            Name = command.Request.Name.Trim(),
            Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description),
            Price = command.Request.Price,
            DurationInDays = command.Request.DurationInDays,
            IncludedVisits = null,
            IsActive = true
        };

        _membershipPlanRepository.Add(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(plan);
    }
}

public sealed record CreateVisitBasedPlanCommand(CreateVisitBasedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class CreateVisitBasedPlanCommandHandler : IRequestHandler<CreateVisitBasedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVisitBasedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(CreateVisitBasedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.IncludedVisits, nameof(command.Request.IncludedVisits));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = new VisitBasedMembershipPlan
        {
            Name = command.Request.Name.Trim(),
            Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description),
            Price = command.Request.Price,
            DurationInDays = null,
            IncludedVisits = command.Request.IncludedVisits,
            IsActive = true
        };

        _membershipPlanRepository.Add(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(plan);
    }
}

public sealed record CreateCombinedPlanCommand(CreateCombinedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class CreateCombinedPlanCommandHandler : IRequestHandler<CreateCombinedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCombinedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(CreateCombinedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.DurationInDays, nameof(command.Request.DurationInDays));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.IncludedVisits, nameof(command.Request.IncludedVisits));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = new CombinedMembershipPlan
        {
            Name = command.Request.Name.Trim(),
            Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description),
            Price = command.Request.Price,
            DurationInDays = command.Request.DurationInDays,
            IncludedVisits = command.Request.IncludedVisits,
            IsActive = true
        };

        _membershipPlanRepository.Add(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(plan);
    }
}

public sealed record UpdateTimeBasedPlanCommand(int PlanId, UpdateTimeBasedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class UpdateTimeBasedPlanCommandHandler : IRequestHandler<UpdateTimeBasedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTimeBasedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(UpdateTimeBasedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.DurationInDays, nameof(command.Request.DurationInDays));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = await _membershipPlanRepository.GetTrackedByIdAsync(command.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Membership plan with id '{command.PlanId}' was not found.");

        var timeBasedPlan = MembershipPlanRequestMappings.EnsurePlanType<TimeBasedMembershipPlan>(plan, MembershipPlanType.TimeBased);
        timeBasedPlan.Name = command.Request.Name.Trim();
        timeBasedPlan.Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description);
        timeBasedPlan.Price = command.Request.Price;
        timeBasedPlan.DurationInDays = command.Request.DurationInDays;
        timeBasedPlan.IncludedVisits = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(timeBasedPlan);
    }
}

public sealed record UpdateVisitBasedPlanCommand(int PlanId, UpdateVisitBasedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class UpdateVisitBasedPlanCommandHandler : IRequestHandler<UpdateVisitBasedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateVisitBasedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(UpdateVisitBasedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.IncludedVisits, nameof(command.Request.IncludedVisits));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = await _membershipPlanRepository.GetTrackedByIdAsync(command.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Membership plan with id '{command.PlanId}' was not found.");

        var visitBasedPlan = MembershipPlanRequestMappings.EnsurePlanType<VisitBasedMembershipPlan>(plan, MembershipPlanType.VisitBased);
        visitBasedPlan.Name = command.Request.Name.Trim();
        visitBasedPlan.Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description);
        visitBasedPlan.Price = command.Request.Price;
        visitBasedPlan.DurationInDays = null;
        visitBasedPlan.IncludedVisits = command.Request.IncludedVisits;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(visitBasedPlan);
    }
}

public sealed record UpdateCombinedPlanCommand(int PlanId, UpdateCombinedPlanRequest Request) : IRequest<MembershipPlanResponse>;

public sealed class UpdateCombinedPlanCommandHandler : IRequestHandler<UpdateCombinedPlanCommand, MembershipPlanResponse>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCombinedPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MembershipPlanResponse> Handle(UpdateCombinedPlanCommand command, CancellationToken cancellationToken)
    {
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.DurationInDays, nameof(command.Request.DurationInDays));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.IncludedVisits, nameof(command.Request.IncludedVisits));
        MembershipPlanRequestMappings.ValidatePositiveValue(command.Request.Price, nameof(command.Request.Price));

        var plan = await _membershipPlanRepository.GetTrackedByIdAsync(command.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Membership plan with id '{command.PlanId}' was not found.");

        var combinedPlan = MembershipPlanRequestMappings.EnsurePlanType<CombinedMembershipPlan>(plan, MembershipPlanType.Combined);
        combinedPlan.Name = command.Request.Name.Trim();
        combinedPlan.Description = MembershipPlanRequestMappings.NormalizeOptionalValue(command.Request.Description);
        combinedPlan.Price = command.Request.Price;
        combinedPlan.DurationInDays = command.Request.DurationInDays;
        combinedPlan.IncludedVisits = command.Request.IncludedVisits;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MembershipPlanRequestMappings.MapResponse(combinedPlan);
    }
}

public sealed record DeactivateMembershipPlanCommand(int PlanId) : IRequest;

public sealed class DeactivateMembershipPlanCommandHandler : IRequestHandler<DeactivateMembershipPlanCommand>
{
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateMembershipPlanCommandHandler(IMembershipPlanRepository membershipPlanRepository, IUnitOfWork unitOfWork)
    {
        _membershipPlanRepository = membershipPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateMembershipPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await _membershipPlanRepository.GetTrackedByIdAsync(command.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Membership plan with id '{command.PlanId}' was not found.");

        plan.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

file static class MembershipPlanRequestMappings
{
    public static MembershipPlanResponse MapResponse(MembershipPlan plan) =>
        new()
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            PlanType = plan.PlanType,
            DurationInDays = plan.DurationInDays,
            IncludedVisits = plan.IncludedVisits,
            IsActive = plan.IsActive
        };

    public static TPlan EnsurePlanType<TPlan>(MembershipPlan plan, MembershipPlanType expectedType)
        where TPlan : MembershipPlan
    {
        if (plan.PlanType != expectedType || plan is not TPlan typedPlan)
        {
            throw new BadRequestException($"Membership plan '{plan.Id}' is not of type '{expectedType}'.");
        }

        return typedPlan;
    }

    public static void ValidatePositiveValue(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BadRequestException($"{fieldName} must be greater than 0.");
        }
    }

    public static void ValidatePositiveValue(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BadRequestException($"{fieldName} must be greater than 0.");
        }
    }

    public static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
