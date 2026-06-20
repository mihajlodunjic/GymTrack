using GymTrack.Enums;

namespace GymTrack.DTOs.Dashboard;

public sealed class SystemNotificationResponse
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public SystemNotificationType Type { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; }
}
