namespace GymTrack.Common.Options;

public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    public bool Enabled { get; init; }
}
