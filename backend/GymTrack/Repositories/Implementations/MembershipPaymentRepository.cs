using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class MembershipPaymentRepository : IMembershipPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public MembershipPaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MembershipPayment>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery(asNoTracking: true)
            .ToListAsync(cancellationToken);

    public Task<MembershipPayment?> GetByIdWithDetailsAsync(int paymentId, CancellationToken cancellationToken = default) =>
        CreateWithDetailsQuery(asNoTracking: true)
            .SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);

    public async Task<IReadOnlyList<MembershipPayment>> GetForMemberWithDetailsAsync(int memberId, CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery(asNoTracking: true)
            .Where(payment => payment.MemberId == memberId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MembershipPayment>> GetForMemberTrackedWithDetailsAsync(int memberId, CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery(asNoTracking: false)
            .Where(payment => payment.MemberId == memberId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MembershipPayment>> GetForMemberForActiveSelectionAsync(int memberId, CancellationToken cancellationToken = default) =>
        await CreateWithDetailsQuery(asNoTracking: true)
            .Where(payment => payment.MemberId == memberId)
            .ToListAsync(cancellationToken);

    public void Add(MembershipPayment payment) =>
        _dbContext.MembershipPayments.Add(payment);

    private IQueryable<MembershipPayment> CreateWithDetailsQuery(bool asNoTracking)
    {
        var query = _dbContext.MembershipPayments
            .Include(payment => payment.Member)
            .Include(payment => payment.MembershipPlan)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }
}
