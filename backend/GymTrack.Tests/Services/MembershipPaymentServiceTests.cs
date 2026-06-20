using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.MembershipPayment;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Tests.Services;

public sealed class MembershipPaymentServiceTests
{
    [Fact]
    public async Task CreatePaymentAsync_AdminCanCreateTimeBasedPayment_AndCopyHistoricalValues()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedTimeBasedPlanAsync(dbContext, durationInDays: 30, price: 3000m);
        var service = CreateService(dbContext);

        var response = await service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = member.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = new DateTime(2026, 6, 20, 12, 30, 0, DateTimeKind.Utc),
                Note = "Placeno u kesu"
            },
            TestClaimsPrincipalFactory.Create(admin));

        var createdPayment = await dbContext.MembershipPayments
            .AsNoTracking()
            .SingleAsync(payment => payment.Id == response.Id);

        Assert.Equal(member.Id, response.MemberId);
        Assert.Equal(plan.Id, response.MembershipPlanId);
        Assert.Equal(3000m, response.Amount);
        Assert.Equal(new DateTime(2026, 6, 20), response.ValidFrom);
        Assert.Equal(new DateTime(2026, 7, 20), response.ValidUntil);
        Assert.Null(response.TotalVisits);
        Assert.Null(response.UsedVisits);
        Assert.Equal(admin.Id, createdPayment.CreatedByUserId);
        Assert.Equal("Placeno u kesu", createdPayment.Note);
    }

    [Fact]
    public async Task CreatePaymentAsync_MemberCannotCreatePayment()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var otherMember = await SeedMemberAsync(dbContext, "ivana@example.com", "GYM-2026-0002");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = otherMember.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = DateTime.UtcNow.Date
            },
            TestClaimsPrincipalFactory.Create(member.User)));

        Assert.Equal("Only admins can create membership payments.", exception.Message);
    }

    [Fact]
    public async Task CreatePaymentAsync_RejectsInactivePlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m, isActive: false);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = member.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = DateTime.UtcNow.Date
            },
            TestClaimsPrincipalFactory.Create(admin)));
    }

    [Fact]
    public async Task CreatePaymentAsync_RejectsInactiveMember()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001", isActive: false);
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = member.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = DateTime.UtcNow.Date
            },
            TestClaimsPrincipalFactory.Create(admin)));
    }

    [Fact]
    public async Task CreatePaymentAsync_InitializesVisitBasedFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 12, price: 2600m);
        var service = CreateService(dbContext);

        var response = await service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = member.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = new DateTime(2026, 6, 20)
            },
            TestClaimsPrincipalFactory.Create(admin));

        Assert.Null(response.ValidUntil);
        Assert.Equal(12, response.TotalVisits);
        Assert.Equal(0, response.UsedVisits);
        Assert.Equal(12, response.RemainingVisits);
    }

    [Fact]
    public async Task CreatePaymentAsync_InitializesCombinedFields()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedCombinedPlanAsync(dbContext, durationInDays: 45, includedVisits: 10, price: 3500m);
        var service = CreateService(dbContext);

        var response = await service.CreatePaymentAsync(
            new CreateMembershipPaymentRequest
            {
                MemberId = member.Id,
                MembershipPlanId = plan.Id,
                ValidFrom = new DateTime(2026, 6, 20)
            },
            TestClaimsPrincipalFactory.Create(admin));

        Assert.Equal(new DateTime(2026, 8, 4), response.ValidUntil);
        Assert.Equal(10, response.TotalVisits);
        Assert.Equal(0, response.UsedVisits);
        Assert.Equal(10, response.RemainingVisits);
    }

    [Fact]
    public async Task GetMembershipStatusForMemberAsync_ReturnsTimeBasedStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var activePayment = await SeedPaymentAsync(
            dbContext,
            member,
            await SeedTimeBasedPlanAsync(dbContext, durationInDays: 30, price: 3000m),
            validFrom: DateTime.UtcNow.Date.AddDays(-5),
            validUntil: DateTime.UtcNow.Date.AddDays(25));
        var service = CreateService(dbContext);

        var response = await service.GetMembershipStatusForMemberAsync(member.Id);

        Assert.True(response.HasActiveMembership);
        Assert.Equal(activePayment.Id, response.ActivePaymentId);
        Assert.Equal(MembershipPlanType.TimeBased, response.PlanType);
        Assert.Null(response.RemainingVisits);
    }

    [Fact]
    public async Task GetMembershipStatusForMemberAsync_ReturnsVisitBasedStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var activePayment = await SeedPaymentAsync(
            dbContext,
            member,
            await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m),
            validFrom: DateTime.UtcNow.Date.AddDays(-30),
            totalVisits: 10,
            usedVisits: 4);
        var service = CreateService(dbContext);

        var response = await service.GetMembershipStatusForMemberAsync(member.Id);

        Assert.True(response.HasActiveMembership);
        Assert.Equal(activePayment.Id, response.ActivePaymentId);
        Assert.Equal(MembershipPlanType.VisitBased, response.PlanType);
        Assert.Equal(6, response.RemainingVisits);
    }

    [Fact]
    public async Task GetMembershipStatusForMemberAsync_ReturnsCombinedStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var activePayment = await SeedPaymentAsync(
            dbContext,
            member,
            await SeedCombinedPlanAsync(dbContext, durationInDays: 45, includedVisits: 10, price: 3500m),
            validFrom: DateTime.UtcNow.Date.AddDays(-10),
            validUntil: DateTime.UtcNow.Date.AddDays(35),
            totalVisits: 10,
            usedVisits: 3);
        var service = CreateService(dbContext);

        var response = await service.GetMembershipStatusForMemberAsync(member.Id);

        Assert.True(response.HasActiveMembership);
        Assert.Equal(activePayment.Id, response.ActivePaymentId);
        Assert.Equal(MembershipPlanType.Combined, response.PlanType);
        Assert.Equal(7, response.RemainingVisits);
    }

    [Fact]
    public async Task GetActiveMembershipForMemberAsync_PrefersTimeBasedOverCombinedAndVisitBased()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var visitPlan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);
        var combinedPlan = await SeedCombinedPlanAsync(dbContext, durationInDays: 45, includedVisits: 10, price: 3500m);
        var timePlan = await SeedTimeBasedPlanAsync(dbContext, durationInDays: 30, price: 3000m);

        await SeedPaymentAsync(
            dbContext,
            member,
            visitPlan,
            validFrom: DateTime.UtcNow.Date.AddDays(-20),
            totalVisits: 10,
            usedVisits: 1);
        await SeedPaymentAsync(
            dbContext,
            member,
            combinedPlan,
            validFrom: DateTime.UtcNow.Date.AddDays(-2),
            validUntil: DateTime.UtcNow.Date.AddDays(10),
            totalVisits: 10,
            usedVisits: 2);
        var timePayment = await SeedPaymentAsync(
            dbContext,
            member,
            timePlan,
            validFrom: DateTime.UtcNow.Date.AddDays(-1),
            validUntil: DateTime.UtcNow.Date.AddDays(29));

        var service = CreateService(dbContext);

        var response = await service.GetActiveMembershipForMemberAsync(member.Id);

        Assert.NotNull(response);
        Assert.Equal(timePayment.Id, response!.Id);
        Assert.Equal(MembershipPlanType.TimeBased, response.PlanType);
    }

    [Fact]
    public async Task GetActiveMembershipForMemberAsync_PicksNearestExpiringCombinedPayment()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedCombinedPlanAsync(dbContext, durationInDays: 45, includedVisits: 10, price: 3500m);

        await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-1),
            validUntil: DateTime.UtcNow.Date.AddDays(20),
            totalVisits: 10,
            usedVisits: 1);
        var nearestPayment = await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-1),
            validUntil: DateTime.UtcNow.Date.AddDays(5),
            totalVisits: 10,
            usedVisits: 1);

        var service = CreateService(dbContext);

        var response = await service.GetActiveMembershipForMemberAsync(member.Id);

        Assert.NotNull(response);
        Assert.Equal(nearestPayment.Id, response!.Id);
    }

    [Fact]
    public async Task GetActiveMembershipForMemberAsync_PicksEarliestPurchasedVisitBasedPayment()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);

        var firstPayment = await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            paidAt: DateTime.UtcNow.AddDays(-5),
            validFrom: DateTime.UtcNow.Date.AddDays(-30),
            totalVisits: 10,
            usedVisits: 4);
        await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            paidAt: DateTime.UtcNow.AddDays(-1),
            validFrom: DateTime.UtcNow.Date.AddDays(-10),
            totalVisits: 10,
            usedVisits: 1);

        var service = CreateService(dbContext);

        var response = await service.GetActiveMembershipForMemberAsync(member.Id);

        Assert.NotNull(response);
        Assert.Equal(firstPayment.Id, response!.Id);
    }

    [Fact]
    public async Task GetCurrentMemberPaymentsAsync_ReturnsOnlyCurrentMemberPayments()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var otherMember = await SeedMemberAsync(dbContext, "ivana@example.com", "GYM-2026-0002");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);

        await SeedPaymentAsync(dbContext, member, plan, validFrom: DateTime.UtcNow.Date.AddDays(-10), totalVisits: 10, usedVisits: 2);
        await SeedPaymentAsync(dbContext, otherMember, plan, validFrom: DateTime.UtcNow.Date.AddDays(-10), totalVisits: 10, usedVisits: 1);

        var service = CreateService(dbContext);

        var response = await service.GetCurrentMemberPaymentsAsync(TestClaimsPrincipalFactory.Create(member.User));

        Assert.Single(response);
        Assert.All(response, payment => Assert.Equal(member.Id, payment.MemberId));
    }

    [Fact]
    public async Task GetCurrentMemberStatusAsync_ReturnsOnlyCurrentMemberStatus()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "marko@example.com", "GYM-2026-0001");
        var otherMember = await SeedMemberAsync(dbContext, "ivana@example.com", "GYM-2026-0002");
        var plan = await SeedVisitBasedPlanAsync(dbContext, includedVisits: 10, price: 2500m);

        var ownPayment = await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-10),
            totalVisits: 10,
            usedVisits: 1);
        await SeedPaymentAsync(
            dbContext,
            otherMember,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-10),
            totalVisits: 10,
            usedVisits: 1);

        var service = CreateService(dbContext);

        var response = await service.GetCurrentMemberStatusAsync(TestClaimsPrincipalFactory.Create(member.User));

        Assert.True(response.HasActiveMembership);
        Assert.Equal(member.Id, response.MemberId);
        Assert.Equal(ownPayment.Id, response.ActivePaymentId);
    }

    private static MembershipPaymentService CreateService(AppDbContext dbContext) =>
        new(dbContext);

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

    private static async Task<Member> SeedMemberAsync(
        AppDbContext dbContext,
        string email,
        string membershipCode,
        bool isActive = true)
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
            FirstName = "Marko",
            LastName = membershipCode.EndsWith("2", StringComparison.Ordinal) ? "Ivanovic" : "Markovic",
            PhoneNumber = "0601234567",
            MembershipCode = membershipCode,
            IsActive = isActive
        };

        user.Member = member;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private static async Task<TimeBasedMembershipPlan> SeedTimeBasedPlanAsync(
        AppDbContext dbContext,
        int durationInDays,
        decimal price,
        bool isActive = true)
    {
        var plan = new TimeBasedMembershipPlan
        {
            Name = $"Time plan {durationInDays}",
            Price = price,
            DurationInDays = durationInDays,
            IsActive = isActive
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    private static async Task<VisitBasedMembershipPlan> SeedVisitBasedPlanAsync(
        AppDbContext dbContext,
        int includedVisits,
        decimal price,
        bool isActive = true)
    {
        var plan = new VisitBasedMembershipPlan
        {
            Name = $"Visit plan {includedVisits}",
            Price = price,
            IncludedVisits = includedVisits,
            IsActive = isActive
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    private static async Task<CombinedMembershipPlan> SeedCombinedPlanAsync(
        AppDbContext dbContext,
        int durationInDays,
        int includedVisits,
        decimal price,
        bool isActive = true)
    {
        var plan = new CombinedMembershipPlan
        {
            Name = $"Combined plan {includedVisits}/{durationInDays}",
            Price = price,
            DurationInDays = durationInDays,
            IncludedVisits = includedVisits,
            IsActive = isActive
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    private static async Task<MembershipPayment> SeedPaymentAsync(
        AppDbContext dbContext,
        Member member,
        MembershipPlan plan,
        DateTime validFrom,
        DateTime? paidAt = null,
        DateTime? validUntil = null,
        int? totalVisits = null,
        int? usedVisits = null)
    {
        var admin = await dbContext.Users.SingleOrDefaultAsync(user => user.Role == UserRole.Admin);
        admin ??= await SeedAdminAsync(dbContext);

        var payment = new MembershipPayment
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            CreatedByUserId = admin.Id,
            Amount = plan.Price,
            PaidAt = paidAt ?? DateTime.UtcNow,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            TotalVisits = totalVisits,
            UsedVisits = usedVisits
        };

        dbContext.MembershipPayments.Add(payment);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(payment).Reference(entity => entity.Member).LoadAsync();
        await dbContext.Entry(payment).Reference(entity => entity.MembershipPlan).LoadAsync();

        return payment;
    }
}
