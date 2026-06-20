using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.CheckIn;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Tests.Services;

public sealed class CheckInServiceTests
{
    [Fact]
    public async Task CheckInByMemberIdAsync_AdminCanCreateCheckInAndLinkPayment()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var payment = await SeedTimeBasedPaymentAsync(dbContext, member);
        var service = new CheckInService(dbContext);

        var response = await service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest { Note = "Jutarnji trening" },
            TestClaimsPrincipalFactory.Create(admin));

        var checkIn = await dbContext.CheckIns
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == response.Id);

        Assert.Equal(member.Id, response.MemberId);
        Assert.Equal(payment.Id, response.MembershipPaymentId);
        Assert.True(response.WasMembershipValid);
        Assert.Equal(payment.Id, checkIn.MembershipPaymentId);
        Assert.Equal(admin.Id, checkIn.CheckedInByUserId);
    }

    [Fact]
    public async Task CheckInByMembershipCodeAsync_AdminCanCreateCheckInByCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        await SeedTimeBasedPaymentAsync(dbContext, member);
        var service = new CheckInService(dbContext);

        var response = await service.CheckInByMembershipCodeAsync(
            member.MembershipCode,
            new CreateCheckInByCodeRequest(),
            TestClaimsPrincipalFactory.Create(admin));

        Assert.Equal(member.Id, response.MemberId);
        Assert.True(response.WasMembershipValid);
    }

    [Fact]
    public async Task CheckInByMemberIdAsync_MemberCannotCreateCheckIn()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        await SeedTimeBasedPaymentAsync(dbContext, member);
        var service = new CheckInService(dbContext);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest(),
            TestClaimsPrincipalFactory.Create(member.User)));
    }

    [Fact]
    public async Task CheckInByMemberIdAsync_RejectsMemberWithoutActiveMembership()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var service = new CheckInService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest(),
            TestClaimsPrincipalFactory.Create(admin)));
    }

    [Fact]
    public async Task CheckInByMemberIdAsync_IncrementsUsedVisitsForVisitBasedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var payment = await SeedVisitBasedPaymentAsync(dbContext, member, totalVisits: 10, usedVisits: 4);
        var service = new CheckInService(dbContext);

        var response = await service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest(),
            TestClaimsPrincipalFactory.Create(admin));

        var updatedPayment = await dbContext.MembershipPayments
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == payment.Id);

        Assert.Equal(5, updatedPayment.UsedVisits);
        Assert.Equal(5, response.RemainingVisits);
    }

    [Fact]
    public async Task CheckInByMemberIdAsync_IncrementsUsedVisitsForCombinedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var payment = await SeedCombinedPaymentAsync(dbContext, member, totalVisits: 8, usedVisits: 2);
        var service = new CheckInService(dbContext);

        var response = await service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest(),
            TestClaimsPrincipalFactory.Create(admin));

        var updatedPayment = await dbContext.MembershipPayments
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == payment.Id);

        Assert.Equal(3, updatedPayment.UsedVisits);
        Assert.Equal(5, response.RemainingVisits);
    }

    [Fact]
    public async Task CheckInByMemberIdAsync_DoesNotChangeVisitsForTimeBasedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var member = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var payment = await SeedTimeBasedPaymentAsync(dbContext, member);
        var service = new CheckInService(dbContext);

        var response = await service.CheckInByMemberIdAsync(
            member.Id,
            new CreateCheckInByMemberIdRequest(),
            TestClaimsPrincipalFactory.Create(admin));

        var updatedPayment = await dbContext.MembershipPayments
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == payment.Id);

        Assert.Null(updatedPayment.TotalVisits);
        Assert.Null(updatedPayment.UsedVisits);
        Assert.Null(response.RemainingVisits);
    }

    [Fact]
    public async Task GetCurrentMemberCheckInsAsync_ReturnsOnlyOwnCheckIns()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var firstMember = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var secondMember = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0002");
        await SeedTimeBasedPaymentAsync(dbContext, firstMember);
        await SeedTimeBasedPaymentAsync(dbContext, secondMember);
        var service = new CheckInService(dbContext);

        await service.CheckInByMemberIdAsync(firstMember.Id, new CreateCheckInByMemberIdRequest(), TestClaimsPrincipalFactory.Create(admin));
        await service.CheckInByMemberIdAsync(secondMember.Id, new CreateCheckInByMemberIdRequest(), TestClaimsPrincipalFactory.Create(admin));

        var response = await service.GetCurrentMemberCheckInsAsync(TestClaimsPrincipalFactory.Create(secondMember.User));

        Assert.Single(response);
        Assert.All(response, checkIn => Assert.Equal(secondMember.Id, checkIn.MemberId));
    }

    [Fact]
    public async Task GetAllCheckInsAsync_ReturnsAllRecordedCheckIns()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var admin = await SeedAdminAsync(dbContext);
        var firstMember = await SeedMemberAsync(dbContext, "pera@example.com", "GYM-2026-0001");
        var secondMember = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0002");
        await SeedTimeBasedPaymentAsync(dbContext, firstMember);
        await SeedTimeBasedPaymentAsync(dbContext, secondMember);
        var service = new CheckInService(dbContext);

        await service.CheckInByMemberIdAsync(firstMember.Id, new CreateCheckInByMemberIdRequest(), TestClaimsPrincipalFactory.Create(admin));
        await service.CheckInByMemberIdAsync(secondMember.Id, new CreateCheckInByMemberIdRequest(), TestClaimsPrincipalFactory.Create(admin));

        var response = await service.GetAllCheckInsAsync();

        Assert.Equal(2, response.Count);
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
            FirstName = membershipCode.EndsWith("2", StringComparison.Ordinal) ? "Mika" : "Pera",
            LastName = membershipCode.EndsWith("2", StringComparison.Ordinal) ? "Mikic" : "Peric",
            MembershipCode = membershipCode,
            IsActive = true
        };

        user.Member = member;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private static async Task<MembershipPayment> SeedTimeBasedPaymentAsync(AppDbContext dbContext, Member member)
    {
        var plan = new TimeBasedMembershipPlan
        {
            Name = "Mesecna",
            Price = 3000m,
            DurationInDays = 30,
            IsActive = true
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        return await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-2),
            validUntil: DateTime.UtcNow.Date.AddDays(28));
    }

    private static async Task<MembershipPayment> SeedVisitBasedPaymentAsync(AppDbContext dbContext, Member member, int totalVisits, int usedVisits)
    {
        var plan = new VisitBasedMembershipPlan
        {
            Name = "10 ulazaka",
            Price = 2500m,
            IncludedVisits = totalVisits,
            IsActive = true
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        return await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-10),
            totalVisits: totalVisits,
            usedVisits: usedVisits);
    }

    private static async Task<MembershipPayment> SeedCombinedPaymentAsync(AppDbContext dbContext, Member member, int totalVisits, int usedVisits)
    {
        var plan = new CombinedMembershipPlan
        {
            Name = "Kombinovana",
            Price = 3500m,
            DurationInDays = 45,
            IncludedVisits = totalVisits,
            IsActive = true
        };

        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync();

        return await SeedPaymentAsync(
            dbContext,
            member,
            plan,
            validFrom: DateTime.UtcNow.Date.AddDays(-5),
            validUntil: DateTime.UtcNow.Date.AddDays(40),
            totalVisits: totalVisits,
            usedVisits: usedVisits);
    }

    private static async Task<MembershipPayment> SeedPaymentAsync(
        AppDbContext dbContext,
        Member member,
        MembershipPlan plan,
        DateTime validFrom,
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
            PaidAt = DateTime.UtcNow,
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
