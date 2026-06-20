namespace GymTrack.DTOs.Auth;

public sealed class LoginResponse
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public CurrentUserResponse User { get; init; } = new();
}
