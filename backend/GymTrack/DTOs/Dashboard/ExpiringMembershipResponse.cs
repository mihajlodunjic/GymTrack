using GymTrack.Enums;

namespace GymTrack.DTOs.Dashboard;

public sealed class ExpiringMembershipResponse
{
    public int MemberId { get; init; }

    public string MemberFullName { get; init; } = string.Empty;

    public string MembershipCode { get; init; } = string.Empty;

    public int MembershipPaymentId { get; init; }

    public string PlanName { get; init; } = string.Empty;

    public MembershipPlanType PlanType { get; init; }

    public DateTime ValidUntil { get; init; }

    public int DaysUntilExpiration { get; init; }

    public int? RemainingVisits { get; init; }
}
