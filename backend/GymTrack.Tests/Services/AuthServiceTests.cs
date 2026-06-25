using GymTrack.Application.Auth;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.Auth;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

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

        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new LoginCommand(new LoginRequest
        {
            Email = "ADMIN@gymtrack.local",
            Password = "Admin123!"
        }));

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

        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<UnauthorizedException>(() => mediator.Send(new LoginCommand(new LoginRequest
        {
            Email = "admin@gymtrack.local",
            Password = "WrongPassword!"
        })));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_WhenUserDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<UnauthorizedException>(() => mediator.Send(new LoginCommand(new LoginRequest
        {
            Email = "missing@gymtrack.local",
            Password = "Admin123!"
        })));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsMemberId_WhenLinkedMemberExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var passwordService = new PasswordService();

        var user = new User
        {
            Email = "member@gymtrack.local",
            PasswordHash = passwordService.HashPassword("Admin123!"),
            Role = UserRole.Member,
            IsActive = true
        };

        var member = new Member
        {
            User = user,
            FirstName = "Mika",
            LastName = "Mikic",
            MembershipCode = "GYM-2026-0001",
            IsActive = true
        };

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();
        var principal = TestClaimsPrincipalFactory.Create(user);

        var response = await mediator.Send(new GetCurrentUserQuery(principal));

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(member.Id, response.MemberId);
        Assert.Equal(UserRole.Member, response.Role);
    }
}
