namespace GymTrack.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(
        int statusCode,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
