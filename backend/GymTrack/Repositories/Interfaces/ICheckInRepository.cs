using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface ICheckInRepository
{
    Task<IReadOnlyList<CheckIn>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckIn>> GetForMemberWithDetailsAsync(int memberId, CancellationToken cancellationToken = default);

    Task<int> CountForDateAsync(DateTime day, CancellationToken cancellationToken = default);

    void Add(CheckIn checkIn);
}
