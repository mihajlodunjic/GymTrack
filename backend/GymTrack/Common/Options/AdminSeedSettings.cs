namespace GymTrack.Common.Options;

public sealed class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
