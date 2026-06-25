using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface ISystemNotificationRepository
{
    Task<IReadOnlyList<SystemNotification>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);

    void Add(SystemNotification notification);
}
