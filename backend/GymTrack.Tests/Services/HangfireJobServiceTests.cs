using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Tests.Services;

public sealed class HangfireJobServiceTests
{
    [Fact]
    public async Task CheckExpiringMembershipsAsync_CreatesNotification()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        await SeedExpiringTimeBasedPaymentAsync(dbContext, member, DateTime.UtcNow.Date.AddDays(3));

        var service = new HangfireJobService(dbContext, new DashboardService(dbContext));

        await service.CheckExpiringMembershipsAsync();

        var notification = await dbContext.SystemNotifications.SingleAsync();

        Assert.Equal(SystemNotificationType.Warning, notification.Type);
        Assert.Contains("Expiring memberships", notification.Title);
        Assert.Contains(member.MembershipCode, notification.Message);
    }

    [Fact]
    public async Task CheckExpiringMembershipsAsync_DoesNotDuplicateDailyNotification()
    {
        await using var dbContext = TestDbContextFactory.Create();
        await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        await SeedExpiringTimeBasedPaymentAsync(dbContext, member, DateTime.UtcNow.Date.AddDays(2));

        var service = new HangfireJobService(dbContext, new DashboardService(dbContext));

        await service.CheckExpiringMembershipsAsync();
        await service.CheckExpiringMembershipsAsync();

        Assert.Equal(1, await dbContext.SystemNotifications.CountAsync());
    }

    [Fact]
    public async Task CreateDailyAdminReportAsync_CreatesReportNotification()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var payment = await SeedExpiringTimeBasedPaymentAsync(dbContext, member, DateTime.UtcNow.Date.AddDays(4));

        dbContext.CheckIns.Add(new CheckIn
        {
            MemberId = member.Id,
            MembershipPaymentId = payment.Id,
            CheckedInByUserId = admin.Id,
            CheckedInAt = DateTime.UtcNow,
            WasMembershipValid = true,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var service = new HangfireJobService(dbContext, new DashboardService(dbContext));

        await service.CreateDailyAdminReportAsync();

        var notification = await dbContext.SystemNotifications.SingleAsync();

        Assert.Equal(SystemNotificationType.Report, notification.Type);
        Assert.Contains("Daily admin report", notification.Title);
        Assert.Contains("Today check-ins", notification.Message);
    }

    private static async Task<User> SeedAdminAsync(AppDbContext dbContext)
    {
        var admin = new User
        {
            Email = "admin@gymtrack.local",
            PasswordHash = "hashed-password",
            Role = UserRole.Admin,
            IsActive = true
        };

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
        return admin;
    }

    private static async Task<Member> SeedMemberAsync(AppDbContext dbContext, string email, string membershipCode)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hashed-password",
            Role = UserRole.Member,
            IsActive = true
        };

        var member = new Member
        {
            User = user,
            FirstName = "Pera",
            LastName = "Peric",
            MembershipCode = membershipCode,
            IsActive = true
        };

        user.Member = member;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private static async Task<MembershipPayment> SeedExpiringTimeBasedPaymentAsync(AppDbContext dbContext, Member member, DateTime validUntil)
    {
        var admin = await dbContext.Users.SingleAsync(user => user.Role == UserRole.Admin);

        var plan = new TimeBasedMembershipPlan
        {
            Name = "Mesecna",
            Price = 3000m,
            DurationInDays = 30,
            IsActive = true
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var payment = new MembershipPayment
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            CreatedByUserId = admin.Id,
            Amount = plan.Price,
            PaidAt = DateTime.UtcNow,
            ValidFrom = DateTime.UtcNow.Date.AddDays(-10),
            ValidUntil = validUntil
        };

        dbContext.MembershipPayments.Add(payment);
        await dbContext.SaveChangesAsync();
        return payment;
    }
}
