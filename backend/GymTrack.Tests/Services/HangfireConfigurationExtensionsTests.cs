using GymTrack.Common;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymTrack.Tests.Services;

public sealed class HangfireConfigurationExtensionsTests
{
    [Fact]
    public void AddConfiguredHangfire_RegistersHangfireServices_WhenEnabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddConfiguredHangfire(configuration, "Server=(localdb)\\MSSQLLocalDB;Database=GymTrackTest;Trusted_Connection=True;");

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IBackgroundJobClient>());
    }

    [Fact]
    public void AddConfiguredHangfire_SkipsHangfireServices_WhenDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddConfiguredHangfire(configuration, "Server=(localdb)\\MSSQLLocalDB;Database=GymTrackTest;Trusted_Connection=True;");

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<IBackgroundJobClient>());
    }
}
