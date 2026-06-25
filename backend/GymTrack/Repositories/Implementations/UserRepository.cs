using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailWithMemberAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .Include(user => user.Member)
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdWithMemberAsync(int userId, bool asNoTracking, CancellationToken cancellationToken = default) =>
        CreateWithMemberQuery(asNoTracking)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, int excludedUserId, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail && user.Id != excludedUserId, cancellationToken);

    public void Add(User user) =>
        _dbContext.Users.Add(user);

    private IQueryable<User> CreateWithMemberQuery(bool asNoTracking)
    {
        var query = _dbContext.Users
            .Include(user => user.Member)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }
}
