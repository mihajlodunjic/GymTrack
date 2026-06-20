namespace GymTrack.Common.Exceptions;

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(StatusCodes.Status400BadRequest, message, errors)
    {
    }
}
