namespace GymTrack.Services;

public interface IAdminSeedService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
