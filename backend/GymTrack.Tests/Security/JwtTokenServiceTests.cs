using System.IdentityModel.Tokens.Jwt;
using GymTrack.Common.Options;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Security;
using Microsoft.Extensions.Options;

namespace GymTrack.Tests.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var service = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "GymTrack.Tests",
            Audience = "GymTrack.Tests.Client",
            Secret = "01234567890123456789012345678901",
            ExpirationMinutes = 60
        }));

        var user = new User
        {
            Id = 42,
            Email = "admin@gymtrack.local",
            Role = UserRole.Admin
        };

        var tokenResult = service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenResult.Token);

        Assert.Equal("42", jwt.Claims.Single(claim => claim.Type == JwtClaimTypes.UserId).Value);
        Assert.Equal("admin@gymtrack.local", jwt.Claims.Single(claim => claim.Type == JwtClaimTypes.Email).Value);
        Assert.Equal(nameof(UserRole.Admin), jwt.Claims.Single(claim => claim.Type == JwtClaimTypes.Role).Value);
        Assert.True(tokenResult.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_IncludesMemberIdClaim_ForMemberUsers()
    {
        var service = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "GymTrack.Tests",
            Audience = "GymTrack.Tests.Client",
            Secret = "01234567890123456789012345678901",
            ExpirationMinutes = 60
        }));

        var user = new User
        {
            Id = 10,
            Email = "member@gymtrack.local",
            Role = UserRole.Member,
            Member = new Member
            {
                Id = 55,
                UserId = 10,
                FirstName = "Mika",
                LastName = "Mikic",
                MembershipCode = "GYM-2026-0001"
            }
        };

        var tokenResult = service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenResult.Token);

        Assert.Equal("55", jwt.Claims.Single(claim => claim.Type == JwtClaimTypes.MemberId).Value);
    }
}
