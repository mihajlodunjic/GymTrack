using GymTrack.Common.Options;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace GymTrack.Services;

public sealed class AdminSeedService : IAdminSeedService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly AdminSeedSettings _settings;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IOptions<AdminSeedSettings> settings,
        ILogger<AdminSeedService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
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

        if (await _userRepository.GetByEmailWithMemberAsync(normalizedEmail, cancellationToken) is not null)
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

        _userRepository.Add(adminUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin seed created user '{Email}'.", normalizedEmail);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
