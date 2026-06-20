using GymTrack.Enums;

namespace GymTrack.DTOs.Auth;

public sealed class CurrentUserResponse
{
    public int Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public int? MemberId { get; init; }
}
