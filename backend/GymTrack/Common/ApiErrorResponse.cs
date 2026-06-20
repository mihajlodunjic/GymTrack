namespace GymTrack.Common;

public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public DateTime Timestamp { get; init; }

    public string Path { get; init; } = string.Empty;
}
