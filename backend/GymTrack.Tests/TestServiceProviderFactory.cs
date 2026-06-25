using GymTrack.Common.Options;
using GymTrack.Data;
using GymTrack.Repositories.Implementations;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using GymTrack.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GymTrack.Tests;

internal static class TestServiceProviderFactory
{
    public static ServiceProvider Create(
        AppDbContext dbContext,
        JwtSettings? jwtSettings = null,
        AdminSeedSettings? adminSeedSettings = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(dbContext);
        services.AddScoped(_ => dbContext);
        services.AddSingleton<IOptions<JwtSettings>>(Options.Create(jwtSettings ?? CreateJwtSettings()));
        services.AddSingleton<IOptions<AdminSeedSettings>>(Options.Create(adminSeedSettings ?? new AdminSeedSettings()));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
        services.AddScoped<IMembershipPaymentRepository, MembershipPaymentRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<ISystemNotificationRepository, SystemNotificationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAdminSeedService, AdminSeedService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IHangfireJobService, HangfireJobService>();

        return services.BuildServiceProvider();
    }

    public static JwtSettings CreateJwtSettings() =>
        new()
        {
            Issuer = "GymTrack.Tests",
            Audience = "GymTrack.Tests.Client",
            Secret = "01234567890123456789012345678901",
            ExpirationMinutes = 60
        };
}
