using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface IMembershipPlanRepository
{
    Task<IReadOnlyList<MembershipPlan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MembershipPlan?> GetByIdAsync(int planId, bool asNoTracking, CancellationToken cancellationToken = default);

    Task<MembershipPlan?> GetTrackedByIdAsync(int planId, CancellationToken cancellationToken = default);

    void Add(MembershipPlan plan);
}
