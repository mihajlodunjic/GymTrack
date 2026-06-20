using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.MembershipPlan;

public sealed class UpdateTimeBasedPlanRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Price { get; init; }

    [Range(1, int.MaxValue)]
    public int DurationInDays { get; init; }
}
