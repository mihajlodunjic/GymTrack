using GymTrack.Common.Exceptions;
using GymTrack.DTOs.Member;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;

namespace GymTrack.Tests.Services;

public sealed class MemberServiceTests
{
    [Fact]
    public async Task CreateMemberAsync_CreatesUserAndMemberProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());

        var response = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            PhoneNumber = "0601234567",
            Password = "StrongPass1!"
        });

        var member = await dbContext.Members.FindAsync(response.Id);
        var user = await dbContext.Users.FindAsync(response.UserId);

        Assert.NotNull(member);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Member, user!.Role);
        Assert.True(user.IsActive);
        Assert.True(member!.IsActive);
        Assert.StartsWith("GYM-", member.MembershipCode);
        Assert.True(new PasswordService().VerifyPassword("StrongPass1!", user.PasswordHash));
    }

    [Fact]
    public async Task CreateMemberAsync_GeneratesUniqueMembershipCodes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());

        var first = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            Password = "StrongPass1!"
        });

        var second = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Mika",
            LastName = "Mikic",
            Email = "mika@gymtrack.local",
            Password = "StrongPass1!"
        });

        Assert.NotEqual(first.MembershipCode, second.MembershipCode);
    }

    [Fact]
    public async Task CreateMemberAsync_RejectsDuplicateEmail()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());

        await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            Password = "StrongPass1!"
        });

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Mika",
            LastName = "Mikic",
            Email = "PERA@gymtrack.local",
            Password = "StrongPass1!"
        }));
    }

    [Fact]
    public async Task UpdateMemberAsync_UpdatesProfileAndEmail()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());
        var created = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            Password = "StrongPass1!"
        });

        var updated = await service.UpdateMemberAsync(created.Id, new UpdateMemberRequest
        {
            FirstName = "Petar",
            LastName = "Petrovic",
            Email = "petar@gymtrack.local",
            PhoneNumber = "0600000000"
        });

        Assert.Equal("Petar", updated.FirstName);
        Assert.Equal("Petrovic", updated.LastName);
        Assert.Equal("petar@gymtrack.local", updated.Email);
        Assert.Equal("0600000000", updated.PhoneNumber);
    }

    [Fact]
    public async Task DeactivateMemberAsync_DeactivatesMemberAndUser()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());
        var created = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            Password = "StrongPass1!"
        });

        await service.DeactivateMemberAsync(created.Id);

        var member = await dbContext.Members.FindAsync(created.Id);
        var user = await dbContext.Users.FindAsync(created.UserId);

        Assert.NotNull(member);
        Assert.NotNull(user);
        Assert.False(member!.IsActive);
        Assert.False(user!.IsActive);
    }

    [Fact]
    public async Task GetCurrentMemberProfileAsync_ReturnsOnlyClaimMemberProfile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new MemberService(dbContext, new PasswordService());

        var first = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Pera",
            LastName = "Peric",
            Email = "pera@gymtrack.local",
            Password = "StrongPass1!"
        });

        var second = await service.CreateMemberAsync(new CreateMemberRequest
        {
            FirstName = "Mika",
            LastName = "Mikic",
            Email = "mika@gymtrack.local",
            Password = "StrongPass1!"
        });

        var user = await dbContext.Users
            .FindAsync(second.UserId);
        var member = await dbContext.Members
            .FindAsync(second.Id);

        Assert.NotNull(user);
        Assert.NotNull(member);

        user!.Member = member!;

        var principal = TestClaimsPrincipalFactory.Create(user);
        var profile = await service.GetCurrentMemberProfileAsync(principal);

        Assert.Equal(second.Id, profile.Id);
        Assert.Equal("mika@gymtrack.local", profile.Email);
        Assert.NotEqual(first.Id, profile.Id);
    }
}
