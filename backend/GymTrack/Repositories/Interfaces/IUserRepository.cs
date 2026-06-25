using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailWithMemberAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithMemberAsync(int userId, bool asNoTracking, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, int excludedUserId, CancellationToken cancellationToken = default);

    void Add(User user);
}
