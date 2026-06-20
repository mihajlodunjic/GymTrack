using System.Security.Claims;
using GymTrack.Entities;
using GymTrack.Security;

namespace GymTrack.Tests;

internal static class TestClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.UserId, user.Id.ToString()),
            new(JwtClaimTypes.Email, user.Email),
            new(JwtClaimTypes.Role, user.Role.ToString())
        };

        if (user.Member is not null)
        {
            claims.Add(new Claim(JwtClaimTypes.MemberId, user.Member.Id.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
