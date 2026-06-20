namespace GymTrack.Common.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(StatusCodes.Status401Unauthorized, message)
    {
    }
}
