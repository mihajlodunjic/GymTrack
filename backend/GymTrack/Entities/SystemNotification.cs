using System.ComponentModel.DataAnnotations;
using GymTrack.Enums;

namespace GymTrack.Entities;

public sealed class SystemNotification
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    public SystemNotificationType Type { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
