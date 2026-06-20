using GymTrack.Services;

namespace GymTrack.Tests.Services;

public sealed class PasswordServiceTests
{
    [Fact]
    public void HashPassword_ReturnsHashThatCanBeVerified()
    {
        var service = new PasswordService();

        var hash = service.HashPassword("Admin123!");

        Assert.NotEqual("Admin123!", hash);
        Assert.True(service.VerifyPassword("Admin123!", hash));
        Assert.False(service.VerifyPassword("WrongPassword!", hash));
    }
}
