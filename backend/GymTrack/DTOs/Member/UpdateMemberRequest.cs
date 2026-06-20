using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.Member;

public sealed class UpdateMemberRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; init; }
}
