namespace GymTrack.DTOs.CheckIn;

public sealed class CheckInResponse
{
    public int Id { get; init; }

    public int MemberId { get; init; }

    public string MemberFullName { get; init; } = string.Empty;

    public int MembershipPaymentId { get; init; }

    public string PlanName { get; init; } = string.Empty;

    public DateTime CheckedInAt { get; init; }

    public bool WasMembershipValid { get; init; }

    public int? RemainingVisits { get; init; }

    public string Message { get; init; } = string.Empty;
}
