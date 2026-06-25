using GymTrack.Application.MembershipPlans;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.MembershipPlan;
using GymTrack.Entities;
using GymTrack.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymTrack.Tests.Services;

public sealed class MembershipPlanServiceTests
{
    [Fact]
    public async Task CreateTimeBasedPlanAsync_CreatesTimeBasedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new CreateTimeBasedPlanCommand(new CreateTimeBasedPlanRequest
        {
            Name = "Mesecna",
            Description = "30 dana",
            Price = 3000,
            DurationInDays = 30
        }));

        Assert.Equal(MembershipPlanType.TimeBased, response.PlanType);
        Assert.Equal(30, response.DurationInDays);
        Assert.Null(response.IncludedVisits);
    }

    [Fact]
    public async Task CreateVisitBasedPlanAsync_CreatesVisitBasedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new CreateVisitBasedPlanCommand(new CreateVisitBasedPlanRequest
        {
            Name = "10 ulazaka",
            Price = 2500,
            IncludedVisits = 10
        }));

        Assert.Equal(MembershipPlanType.VisitBased, response.PlanType);
        Assert.Equal(10, response.IncludedVisits);
        Assert.Null(response.DurationInDays);
    }

    [Fact]
    public async Task CreateCombinedPlanAsync_CreatesCombinedPlan()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new CreateCombinedPlanCommand(new CreateCombinedPlanRequest
        {
            Name = "10 ulazaka / 45 dana",
            Price = 3500,
            DurationInDays = 45,
            IncludedVisits = 10
        }));

        Assert.Equal(MembershipPlanType.Combined, response.PlanType);
        Assert.Equal(45, response.DurationInDays);
        Assert.Equal(10, response.IncludedVisits);
    }

    [Fact]
    public async Task CreateTimeBasedPlanAsync_RejectsInvalidValues()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<BadRequestException>(() => mediator.Send(new CreateTimeBasedPlanCommand(new CreateTimeBasedPlanRequest
        {
            Name = "Neispravan",
            Price = 1000,
            DurationInDays = 0
        })));
    }

    [Fact]
    public async Task DeactivatePlanAsync_SoftDeactivatesPlanWithoutDeletingIt()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var created = await mediator.Send(new CreateCombinedPlanCommand(new CreateCombinedPlanRequest
        {
            Name = "Kombinovani",
            Price = 3500,
            DurationInDays = 45,
            IncludedVisits = 10
        }));

        await mediator.Send(new DeactivateMembershipPlanCommand(created.Id));

        var plan = await dbContext.MembershipPlans.SingleOrDefaultAsync(entity => entity.Id == created.Id);

        Assert.NotNull(plan);
        Assert.False(plan!.IsActive);
    }

    [Fact]
    public async Task Model_StoresDifferentPlanTypesInSingleSet()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.MembershipPlans.AddRange(
            new TimeBasedMembershipPlan
            {
                Name = "Mesecna",
                Price = 3000,
                DurationInDays = 30
            },
            new VisitBasedMembershipPlan
            {
                Name = "10 ulazaka",
                Price = 2500,
                IncludedVisits = 10
            },
            new CombinedMembershipPlan
            {
                Name = "Kombinovana",
                Price = 3500,
                DurationInDays = 45,
                IncludedVisits = 10
            });

        await dbContext.SaveChangesAsync();

        var plans = await dbContext.MembershipPlans.OrderBy(plan => plan.Id).ToListAsync();

        Assert.Collection(plans,
            plan => Assert.Equal(MembershipPlanType.TimeBased, plan.PlanType),
            plan => Assert.Equal(MembershipPlanType.VisitBased, plan.PlanType),
            plan => Assert.Equal(MembershipPlanType.Combined, plan.PlanType));
    }
}
