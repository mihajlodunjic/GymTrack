using System.ComponentModel.DataAnnotations;
using GymTrack.Common;

namespace GymTrack.Entities;

public sealed class Member : IAuditableEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string MembershipCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<MembershipPayment> MembershipPayments { get; set; } = new List<MembershipPayment>();
}
