using System.ComponentModel.DataAnnotations;

namespace GymTrack.Entities;

public sealed class CheckIn
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int MembershipPaymentId { get; set; }

    public int CheckedInByUserId { get; set; }

    public DateTime CheckedInAt { get; set; }

    public bool WasMembershipValid { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Member Member { get; set; } = null!;

    public MembershipPayment MembershipPayment { get; set; } = null!;

    public User CheckedInByUser { get; set; } = null!;
}
