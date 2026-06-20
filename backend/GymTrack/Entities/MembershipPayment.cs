using System.ComponentModel.DataAnnotations;
using GymTrack.Common;

namespace GymTrack.Entities;

public sealed class MembershipPayment : IAuditableEntity
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int MembershipPlanId { get; set; }

    public int CreatedByUserId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public int? TotalVisits { get; set; }

    public int? UsedVisits { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Member Member { get; set; } = null!;

    public MembershipPlan MembershipPlan { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
