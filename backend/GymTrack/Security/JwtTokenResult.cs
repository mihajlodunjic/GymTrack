namespace GymTrack.Security;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAt);
