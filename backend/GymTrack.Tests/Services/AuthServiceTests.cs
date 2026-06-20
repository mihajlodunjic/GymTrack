using GymTrack.Common.Exceptions;
using GymTrack.Common.Options;
using GymTrack.DTOs.Auth;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Security;
using GymTrack.Services;
using Microsoft.Extensions.Options;

namespace GymTrack.Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsToken_ForValidCredentials()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var passwordService = new PasswordService();

        dbContext.Users.Add(new User
        {
            Email = "admin@gymtrack.local",
            PasswordHash = passwordService.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, passwordService, CreateTokenService());

        var response = await authService.LoginAsync(new LoginRequest
        {
            Email = "ADMIN@gymtrack.local",
            Password = "Admin123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("admin@gymtrack.local", response.User.Email);
        Assert.Equal(UserRole.Admin, response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenPasswordIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var passwordService = new PasswordService();

        dbContext.Users.Add(new User
        {
            Email = "admin@gymtrack.local",
            PasswordHash = passwordService.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, passwordService, CreateTokenService());

        await Assert.ThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequest
        {
            Email = "admin@gymtrack.local",
            Password = "WrongPassword!"
        }));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenUserDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var authService = new AuthService(dbContext, new PasswordService(), CreateTokenService());

        await Assert.ThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequest
        {
            Email = "missing@gymtrack.local",
            Password = "Admin123!"
        }));
    }

    private static ITokenService CreateTokenService()
    {
        return new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "GymTrack.Tests",
            Audience = "GymTrack.Tests.Client",
            Secret = "01234567890123456789012345678901",
            ExpirationMinutes = 60
        }));
    }
}
