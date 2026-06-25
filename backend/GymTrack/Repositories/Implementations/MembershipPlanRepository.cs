using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class MembershipPlanRepository : IMembershipPlanRepository
{
    private readonly AppDbContext _dbContext;

    public MembershipPlanRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.MembershipPlans
            .AsNoTracking()
            .OrderBy(plan => plan.Name)
            .ToListAsync(cancellationToken);

    public Task<MembershipPlan?> GetByIdAsync(int planId, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MembershipPlans.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(plan => plan.Id == planId, cancellationToken);
    }

    public Task<MembershipPlan?> GetTrackedByIdAsync(int planId, CancellationToken cancellationToken = default) =>
        _dbContext.MembershipPlans
            .SingleOrDefaultAsync(plan => plan.Id == planId, cancellationToken);

    public void Add(MembershipPlan plan) =>
        _dbContext.MembershipPlans.Add(plan);
}
