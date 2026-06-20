using GymTrack.Entities;

namespace GymTrack.Security;

public interface ITokenService
{
    JwtTokenResult GenerateToken(User user);
}
