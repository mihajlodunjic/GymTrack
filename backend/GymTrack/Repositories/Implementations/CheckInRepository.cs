using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class CheckInRepository : ICheckInRepository
{
    private readonly AppDbContext _dbContext;

    public CheckInRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CheckIn>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CheckIn>> GetForMemberWithDetailsAsync(int memberId, CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery()
            .Where(checkIn => checkIn.MemberId == memberId)
            .ToListAsync(cancellationToken);

    public Task<int> CountForDateAsync(DateTime day, CancellationToken cancellationToken = default)
    {
        var date = day.Date;

        return _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(checkIn => checkIn.CheckedInAt >= date && checkIn.CheckedInAt < date.AddDays(1), cancellationToken);
    }

    public void Add(CheckIn checkIn) =>
        _dbContext.CheckIns.Add(checkIn);

    private IQueryable<CheckIn> CreateWithDetailsQuery() =>
        _dbContext.CheckIns
            .AsNoTracking()
            .Include(checkIn => checkIn.Member)
            .Include(checkIn => checkIn.MembershipPayment)
            .ThenInclude(payment => payment.MembershipPlan);
}
