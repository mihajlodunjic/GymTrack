namespace GymTrack.Common.Options;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public const string PolicyName = "ConfiguredOrigins";

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
