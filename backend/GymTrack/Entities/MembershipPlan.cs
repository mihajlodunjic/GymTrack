using System.ComponentModel.DataAnnotations;
using GymTrack.Common;
using GymTrack.Enums;

namespace GymTrack.Entities;

public abstract class MembershipPlan : IAuditableEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public MembershipPlanType PlanType { get; protected set; }

    public int? DurationInDays { get; set; }

    public int? IncludedVisits { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
