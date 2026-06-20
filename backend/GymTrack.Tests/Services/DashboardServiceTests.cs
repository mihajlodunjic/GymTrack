using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Tests.Services;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsExpectedCounts()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var admin = await SeedAdminAsync(dbContext);
        var activeMemberOne = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001", isActive: true);
        var activeMemberTwo = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0002", isActive: true);
        var activeMemberThree = await SeedMemberAsync(dbContext, "zika@example.com", "GYM-2026-0003", isActive: true);
        await SeedMemberAsync(dbContext, "inactive@example.com", "GYM-2026-0004", isActive: false);

        var activeTimeBased = await SeedPaymentAsync(
            dbContext,
            activeMemberOne,
            new TimeBasedMembershipPlan
            {
                Name = "Mesecna",
                Price = 3000m,
                DurationInDays = 30,
                IsActive = true
            },
            paidAt: monthStart.AddDays(1),
            validFrom: today.AddDays(-5),
            validUntil: today.AddDays(5));

        var activeCombined = await SeedPaymentAsync(
            dbContext,
            activeMemberTwo,
            new CombinedMembershipPlan
            {
                Name = "10/45",
                Price = 3500m,
                DurationInDays = 45,
                IncludedVisits = 10,
                IsActive = true
            },
            paidAt: monthStart.AddDays(2),
            validFrom: today.AddDays(-3),
            validUntil: today.AddDays(3),
            totalVisits: 10,
            usedVisits: 2);

        var expiredVisitBased = await SeedPaymentAsync(
            dbContext,
            activeMemberThree,
            new VisitBasedMembershipPlan
            {
                Name = "10 ulazaka",
                Price = 2500m,
                IncludedVisits = 10,
                IsActive = true
            },
            paidAt: monthStart.AddDays(3),
            validFrom: today.AddDays(-20),
            totalVisits: 10,
            usedVisits: 10);

        dbContext.CheckIns.Add(new CheckIn
        {
            MemberId = activeMemberOne.Id,
            MembershipPaymentId = activeTimeBased.Id,
            CheckedInByUserId = admin.Id,
            CheckedInAt = today.AddHours(9),
            WasMembershipValid = true,
            CreatedAt = today.AddHours(9)
        });

        dbContext.CheckIns.Add(new CheckIn
        {
            MemberId = activeMemberTwo.Id,
            MembershipPaymentId = activeCombined.Id,
            CheckedInByUserId = admin.Id,
            CheckedInAt = today.AddDays(-1).AddHours(10),
            WasMembershipValid = true,
            CreatedAt = today.AddDays(-1).AddHours(10)
        });

        await dbContext.SaveChangesAsync();

        var service = new DashboardService(dbContext);
        var response = await service.GetDashboardStatsAsync();

        Assert.Equal(4, response.TotalMembers);
        Assert.Equal(3, response.ActiveMembers);
        Assert.Equal(1, response.InactiveMembers);
        Assert.Equal(2, response.ActiveMemberships);
        Assert.Equal(1, response.ExpiredMemberships);
        Assert.Equal(1, response.TodayCheckIns);
        Assert.Equal(3, response.CurrentMonthPayments);
        Assert.Equal(9000m, response.CurrentMonthRevenue);
        Assert.Equal(2, response.ExpiringInNextSevenDays);
    }

    [Fact]
    public async Task GetExpiringMembershipsAsync_ReturnsOnlyActiveMembershipsExpiringSoon()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var today = DateTime.UtcNow.Date;

        var firstMember = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001", isActive: true);
        var secondMember = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0002", isActive: true);
        var thirdMember = await SeedMemberAsync(dbContext, "zika@example.com", "GYM-2026-0003", isActive: true);

        await SeedPaymentAsync(
            dbContext,
            firstMember,
            new TimeBasedMembershipPlan
            {
                Name = "Mesecna",
                Price = 3000m,
                DurationInDays = 30,
                IsActive = true
            },
            paidAt: DateTime.UtcNow,
            validFrom: today.AddDays(-10),
            validUntil: today.AddDays(2));

        await SeedPaymentAsync(
            dbContext,
            secondMember,
            new CombinedMembershipPlan
            {
                Name = "10/45",
                Price = 3500m,
                DurationInDays = 45,
                IncludedVisits = 10,
                IsActive = true
            },
            paidAt: DateTime.UtcNow,
            validFrom: today.AddDays(-5),
            validUntil: today.AddDays(6),
            totalVisits: 10,
            usedVisits: 1);

        await SeedPaymentAsync(
            dbContext,
            thirdMember,
            new CombinedMembershipPlan
            {
                Name = "Potrosena",
                Price = 3500m,
                DurationInDays = 45,
                IncludedVisits = 10,
                IsActive = true
            },
            paidAt: DateTime.UtcNow,
            validFrom: today.AddDays(-5),
            validUntil: today.AddDays(1),
            totalVisits: 10,
            usedVisits: 10);

        var service = new DashboardService(dbContext);
        var response = await service.GetExpiringMembershipsAsync();

        Assert.Equal(2, response.Count);
        Assert.Collection(response,
            first => Assert.Equal("GYM-2026-0001", first.MembershipCode),
            second => Assert.Equal("GYM-2026-0002", second.MembershipCode));
        Assert.All(response, membership => Assert.InRange(membership.DaysUntilExpiration, 0, 7));
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

    private static async Task<Member> SeedMemberAsync(AppDbContext dbContext, string email, string membershipCode, bool isActive)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hashed-password",
            Role = UserRole.Member,
            IsActive = isActive
        };

        var member = new Member
        {
            User = user,
            FirstName = membershipCode.EndsWith("2", StringComparison.Ordinal) ? "Mika" : membershipCode.EndsWith("3", StringComparison.Ordinal) ? "Zika" : "Pera",
            LastName = membershipCode.EndsWith("2", StringComparison.Ordinal) ? "Mikic" : membershipCode.EndsWith("3", StringComparison.Ordinal) ? "Zikic" : "Peric",
            MembershipCode = membershipCode,
            IsActive = isActive
        };

        user.Member = member;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private static async Task<MembershipPayment> SeedPaymentAsync(
        AppDbContext dbContext,
        Member member,
        MembershipPlan plan,
        DateTime paidAt,
        DateTime validFrom,
        DateTime? validUntil = null,
        int? totalVisits = null,
        int? usedVisits = null)
    {
        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        var admin = await dbContext.Users.SingleOrDefaultAsync(user => user.Role == UserRole.Admin);
        admin ??= await SeedAdminAsync(dbContext);

        var payment = new MembershipPayment
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            CreatedByUserId = admin.Id,
            Amount = plan.Price,
            PaidAt = paidAt,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            TotalVisits = totalVisits,
            UsedVisits = usedVisits
        };

        dbContext.MembershipPayments.Add(payment);
        await dbContext.SaveChangesAsync();
        return payment;
    }
}
