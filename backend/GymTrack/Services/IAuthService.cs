using System.Security.Claims;
using GymTrack.DTOs.Auth;

namespace GymTrack.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
