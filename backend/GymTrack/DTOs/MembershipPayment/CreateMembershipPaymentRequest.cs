using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.MembershipPayment;

public sealed class CreateMembershipPaymentRequest
{
    [Range(1, int.MaxValue)]
    public int MemberId { get; init; }

    [Range(1, int.MaxValue)]
    public int MembershipPlanId { get; init; }

    public DateTime ValidFrom { get; init; }

    [MaxLength(1000)]
    public string? Note { get; init; }
}
