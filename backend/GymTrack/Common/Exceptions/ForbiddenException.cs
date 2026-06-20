namespace GymTrack.Common.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(StatusCodes.Status403Forbidden, message)
    {
    }
}
