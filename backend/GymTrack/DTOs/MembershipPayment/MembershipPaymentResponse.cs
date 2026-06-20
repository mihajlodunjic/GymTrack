using GymTrack.Enums;

namespace GymTrack.DTOs.MembershipPayment;

public sealed class MembershipPaymentResponse
{
    public int Id { get; init; }

    public int MemberId { get; init; }

    public string MemberFullName { get; init; } = string.Empty;

    public int MembershipPlanId { get; init; }

    public string PlanName { get; init; } = string.Empty;

    public MembershipPlanType PlanType { get; init; }

    public decimal Amount { get; init; }

    public DateTime PaidAt { get; init; }

    public DateTime ValidFrom { get; init; }

    public DateTime? ValidUntil { get; init; }

    public int? TotalVisits { get; init; }

    public int? UsedVisits { get; init; }

    public int? RemainingVisits { get; init; }

    public string? Note { get; init; }
}
