using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class SystemNotificationRepository : ISystemNotificationRepository
{
    private readonly AppDbContext _dbContext;

    public SystemNotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SystemNotification>> GetRecentAsync(int take, CancellationToken cancellationToken = default) =>
        await _dbContext.SystemNotifications
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default) =>
        _dbContext.SystemNotifications
            .AsNoTracking()
            .AnyAsync(notification => notification.Title == title, cancellationToken);

    public void Add(SystemNotification notification) =>
        _dbContext.SystemNotifications.Add(notification);
}
