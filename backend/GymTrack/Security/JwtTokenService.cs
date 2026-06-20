using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymTrack.Common.Options;
using GymTrack.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GymTrack.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public JwtTokenResult GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

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

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            SecurityAlgorithms.HmacSha256);

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return new JwtTokenResult(token, expiresAt);
    }
}
