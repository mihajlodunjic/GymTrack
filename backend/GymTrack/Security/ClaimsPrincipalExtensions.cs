using System.Security.Claims;
using GymTrack.Common.Exceptions;

namespace GymTrack.Security;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(JwtClaimTypes.UserId);
        if (!int.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedException("Current user is not authenticated.");
        }

        return userId;
    }

    public static int GetRequiredMemberId(this ClaimsPrincipal principal)
    {
        var rawMemberId = principal.FindFirstValue(JwtClaimTypes.MemberId);
        if (!int.TryParse(rawMemberId, out var memberId))
        {
            throw new UnauthorizedException("Current member is not authenticated.");
        }

        return memberId;
    }
}
