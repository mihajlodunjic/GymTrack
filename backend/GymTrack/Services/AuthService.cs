using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.Auth;
using GymTrack.Entities;
using GymTrack.Security;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthService(
        AppDbContext dbContext,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        var tokenResult = _tokenService.GenerateToken(user);

        return new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = MapCurrentUser(user)
        };
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.GetRequiredUserId();

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Current user could not be resolved.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        return MapCurrentUser(user);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static CurrentUserResponse MapCurrentUser(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            MemberId = null
        };
}
