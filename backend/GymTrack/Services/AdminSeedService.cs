using GymTrack.Common.Options;
using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymTrack.Services;

public sealed class AdminSeedService : IAdminSeedService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly AdminSeedSettings _settings;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IOptions<AdminSeedSettings> settings,
        ILogger<AdminSeedService> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Admin seed is disabled.");
            return;
        }

        var normalizedEmail = NormalizeEmail(_settings.Email);

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            _logger.LogInformation("Admin seed skipped because user '{Email}' already exists.", normalizedEmail);
            return;
        }

        var adminUser = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(_settings.Password),
            Role = UserRole.Admin,
            IsActive = true
        };

        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin seed created user '{Email}'.", normalizedEmail);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
