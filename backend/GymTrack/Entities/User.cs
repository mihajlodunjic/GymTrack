using System.ComponentModel.DataAnnotations;
using GymTrack.Common;
using GymTrack.Enums;

namespace GymTrack.Entities;

public sealed class User : IAuditableEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Member? Member { get; set; }

    public ICollection<MembershipPayment> CreatedMembershipPayments { get; set; } = new List<MembershipPayment>();
}
