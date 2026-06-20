using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.MembershipPlan;
using GymTrack.Entities;
using GymTrack.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class MembershipPlanService : IMembershipPlanService
{
    private readonly AppDbContext _dbContext;

    public MembershipPlanService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MembershipPlanResponse>> GetAllPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.MembershipPlans
            .AsNoTracking()
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(MapResponse).ToArray();
    }

    public async Task<MembershipPlanResponse> GetPlanByIdAsync(int planId, CancellationToken cancellationToken = default)
    {
        var plan = await FindPlanByIdAsync(planId, cancellationToken);
        return MapResponse(plan);
    }

    public async Task<MembershipPlanResponse> CreateTimeBasedPlanAsync(CreateTimeBasedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.DurationInDays, nameof(request.DurationInDays));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = new TimeBasedMembershipPlan
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptionalValue(request.Description),
            Price = request.Price,
            DurationInDays = request.DurationInDays,
            IncludedVisits = null,
            IsActive = true
        };

        _dbContext.MembershipPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapResponse(plan);
    }

    public async Task<MembershipPlanResponse> CreateVisitBasedPlanAsync(CreateVisitBasedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.IncludedVisits, nameof(request.IncludedVisits));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = new VisitBasedMembershipPlan
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptionalValue(request.Description),
            Price = request.Price,
            DurationInDays = null,
            IncludedVisits = request.IncludedVisits,
            IsActive = true
        };

        _dbContext.MembershipPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapResponse(plan);
    }

    public async Task<MembershipPlanResponse> CreateCombinedPlanAsync(CreateCombinedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.DurationInDays, nameof(request.DurationInDays));
        ValidatePositiveValue(request.IncludedVisits, nameof(request.IncludedVisits));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = new CombinedMembershipPlan
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptionalValue(request.Description),
            Price = request.Price,
            DurationInDays = request.DurationInDays,
            IncludedVisits = request.IncludedVisits,
            IsActive = true
        };

        _dbContext.MembershipPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapResponse(plan);
    }

    public async Task<MembershipPlanResponse> UpdateTimeBasedPlanAsync(int planId, UpdateTimeBasedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.DurationInDays, nameof(request.DurationInDays));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = await FindTrackedPlanByIdAsync(planId, cancellationToken);
        var timeBasedPlan = EnsurePlanType<TimeBasedMembershipPlan>(plan, MembershipPlanType.TimeBased);

        timeBasedPlan.Name = request.Name.Trim();
        timeBasedPlan.Description = NormalizeOptionalValue(request.Description);
        timeBasedPlan.Price = request.Price;
        timeBasedPlan.DurationInDays = request.DurationInDays;
        timeBasedPlan.IncludedVisits = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapResponse(timeBasedPlan);
    }

    public async Task<MembershipPlanResponse> UpdateVisitBasedPlanAsync(int planId, UpdateVisitBasedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.IncludedVisits, nameof(request.IncludedVisits));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = await FindTrackedPlanByIdAsync(planId, cancellationToken);
        var visitBasedPlan = EnsurePlanType<VisitBasedMembershipPlan>(plan, MembershipPlanType.VisitBased);

        visitBasedPlan.Name = request.Name.Trim();
        visitBasedPlan.Description = NormalizeOptionalValue(request.Description);
        visitBasedPlan.Price = request.Price;
        visitBasedPlan.DurationInDays = null;
        visitBasedPlan.IncludedVisits = request.IncludedVisits;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapResponse(visitBasedPlan);
    }

    public async Task<MembershipPlanResponse> UpdateCombinedPlanAsync(int planId, UpdateCombinedPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositiveValue(request.DurationInDays, nameof(request.DurationInDays));
        ValidatePositiveValue(request.IncludedVisits, nameof(request.IncludedVisits));
        ValidatePositiveValue(request.Price, nameof(request.Price));

        var plan = await FindTrackedPlanByIdAsync(planId, cancellationToken);
        var combinedPlan = EnsurePlanType<CombinedMembershipPlan>(plan, MembershipPlanType.Combined);

        combinedPlan.Name = request.Name.Trim();
        combinedPlan.Description = NormalizeOptionalValue(request.Description);
        combinedPlan.Price = request.Price;
        combinedPlan.DurationInDays = request.DurationInDays;
        combinedPlan.IncludedVisits = request.IncludedVisits;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapResponse(combinedPlan);
    }

    public async Task DeactivatePlanAsync(int planId, CancellationToken cancellationToken = default)
    {
        var plan = await FindTrackedPlanByIdAsync(planId, cancellationToken);
        plan.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MembershipPlan> FindPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.MembershipPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        if (plan is null)
        {
            throw new NotFoundException($"Membership plan with id '{planId}' was not found.");
        }

        return plan;
    }

    private async Task<MembershipPlan> FindTrackedPlanByIdAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.MembershipPlans
            .SingleOrDefaultAsync(entity => entity.Id == planId, cancellationToken);

        if (plan is null)
        {
            throw new NotFoundException($"Membership plan with id '{planId}' was not found.");
        }

        return plan;
    }

    private static TPlan EnsurePlanType<TPlan>(MembershipPlan plan, MembershipPlanType expectedType)
        where TPlan : MembershipPlan
    {
        if (plan.PlanType != expectedType || plan is not TPlan typedPlan)
        {
            throw new BadRequestException($"Membership plan '{plan.Id}' is not of type '{expectedType}'.");
        }

        return typedPlan;
    }

    private static MembershipPlanResponse MapResponse(MembershipPlan plan) =>
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

    private static void ValidatePositiveValue(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BadRequestException($"{fieldName} must be greater than 0.");
        }
    }

    private static void ValidatePositiveValue(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new BadRequestException($"{fieldName} must be greater than 0.");
        }
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
