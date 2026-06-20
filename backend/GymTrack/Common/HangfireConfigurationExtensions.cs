using GymTrack.Common.Options;
using Hangfire;
using Hangfire.SqlServer;

namespace GymTrack.Common;

public static class HangfireConfigurationExtensions
{
    public static bool IsHangfireEnabled(this IConfiguration configuration) =>
        configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>()?.Enabled ?? false;

    public static IServiceCollection AddConfiguredHangfire(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        if (!configuration.IsHangfireEnabled())
        {
            return services;
        }

        services.AddHangfire(options => options
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer();

        return services;
    }
}
