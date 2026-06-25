using GymTrack.Common.Options;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Implementations;
using GymTrack.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GymTrack.Tests.Services;

public sealed class AdminSeedServiceTests
{
    [Fact]
    public async Task SeedAsync_DoesNotDuplicateExistingAdminUser()
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

        var service = new AdminSeedService(
            new UserRepository(dbContext),
            new EfUnitOfWork(dbContext),
            passwordService,
            Options.Create(new AdminSeedSettings
            {
                Enabled = true,
                Email = "admin@gymtrack.local",
                Password = "Admin123!"
            }),
            NullLogger<AdminSeedService>.Instance);

        await service.SeedAsync();

        var adminCount = await dbContext.Users.CountAsync(user => user.Email == "admin@gymtrack.local");
        Assert.Equal(1, adminCount);
    }
}
