using GymTrack.Enums;

namespace GymTrack.DTOs.MembershipPlan;

public sealed class MembershipPlanResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public MembershipPlanType PlanType { get; init; }

    public int? DurationInDays { get; init; }

    public int? IncludedVisits { get; init; }

    public bool IsActive { get; init; }
}
