using GymTrack.Enums;

namespace GymTrack.DTOs.MembershipPayment;

public sealed class MembershipStatusResponse
{
    public int MemberId { get; init; }

    public string MemberFullName { get; init; } = string.Empty;

    public string MembershipCode { get; init; } = string.Empty;

    public bool HasActiveMembership { get; init; }

    public int? ActivePaymentId { get; init; }

    public string? PlanName { get; init; }

    public MembershipPlanType? PlanType { get; init; }

    public DateTime? ValidFrom { get; init; }

    public DateTime? ValidUntil { get; init; }

    public int? TotalVisits { get; init; }

    public int? UsedVisits { get; init; }

    public int? RemainingVisits { get; init; }

    public string Message { get; init; } = string.Empty;
}
