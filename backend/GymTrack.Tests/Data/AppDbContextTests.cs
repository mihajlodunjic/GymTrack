using GymTrack.Entities;

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
}
