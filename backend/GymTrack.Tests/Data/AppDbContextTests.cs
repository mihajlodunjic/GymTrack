using GymTrack.Entities;
using GymTrack.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Tests.Data;

public sealed class AppDbContextTests
{
    [Fact]
    public void Model_ConfiguresUniqueIndex_ForUserEmail()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entityType = dbContext.Model.FindEntityType(typeof(User));

        Assert.NotNull(entityType);
        Assert.Contains(
            entityType!.GetIndexes(),
            index => index.IsUnique && index.Properties.Any(property => property.Name == nameof(User.Email)));
    }

    [Fact]
    public void Model_ConfiguresUniqueIndex_ForMembershipCode()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entityType = dbContext.Model.FindEntityType(typeof(Member));

        Assert.NotNull(entityType);
        Assert.Contains(
            entityType!.GetIndexes(),
            index => index.IsUnique && index.Properties.Any(property => property.Name == nameof(Member.MembershipCode)));
    }

    [Fact]
    public void Model_UsesTphMapping_ForMembershipPlans()
    {
        using var dbContext = TestDbContextFactory.Create();

        var baseType = dbContext.Model.FindEntityType(typeof(MembershipPlan));
        var timeBasedType = dbContext.Model.FindEntityType(typeof(TimeBasedMembershipPlan));
        var visitBasedType = dbContext.Model.FindEntityType(typeof(VisitBasedMembershipPlan));
        var combinedType = dbContext.Model.FindEntityType(typeof(CombinedMembershipPlan));

        Assert.NotNull(baseType);
        Assert.NotNull(timeBasedType);
        Assert.NotNull(visitBasedType);
        Assert.NotNull(combinedType);

        Assert.Equal("MembershipPlans", baseType!.GetTableName());
        Assert.Equal("MembershipPlans", timeBasedType!.GetTableName());
        Assert.Equal("MembershipPlans", visitBasedType!.GetTableName());
        Assert.Equal("MembershipPlans", combinedType!.GetTableName());

        var discriminator = baseType.FindDiscriminatorProperty();

        Assert.NotNull(discriminator);
        Assert.Equal(nameof(MembershipPlan.PlanType), discriminator!.Name);
        Assert.Equal(typeof(MembershipPlanType), discriminator.ClrType);
    }

    [Fact]
    public void Model_ConfiguresMembershipPayment_Relationships()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entityType = dbContext.Model.FindEntityType(typeof(MembershipPayment));

        Assert.NotNull(entityType);
        Assert.Equal("MembershipPayments", entityType!.GetTableName());
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(MembershipPayment.MemberId)));
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(MembershipPayment.MembershipPlanId)));
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(MembershipPayment.CreatedByUserId)));
    }

    [Fact]
    public void Model_ConfiguresCheckIn_Relationships()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entityType = dbContext.Model.FindEntityType(typeof(CheckIn));

        Assert.NotNull(entityType);
        Assert.Equal("CheckIns", entityType!.GetTableName());
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(CheckIn.MemberId)));
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(CheckIn.MembershipPaymentId)));
        Assert.Contains(entityType.GetForeignKeys(), key => key.Properties.Any(property => property.Name == nameof(CheckIn.CheckedInByUserId)));
    }
}
