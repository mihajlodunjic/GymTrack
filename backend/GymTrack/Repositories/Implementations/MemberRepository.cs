using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _dbContext;

    public MemberRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Member>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = CreateWithUserQuery(asNoTracking: true);

        if (isActive.HasValue)
        {
            query = query.Where(member => member.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();

            query = query.Where(member =>
                member.FirstName.ToLower().Contains(normalizedSearch) ||
                member.LastName.ToLower().Contains(normalizedSearch) ||
                member.User.Email.ToLower().Contains(normalizedSearch) ||
                (member.PhoneNumber != null && member.PhoneNumber.ToLower().Contains(normalizedSearch)) ||
                member.MembershipCode.ToLower().Contains(normalizedSearch));
        }

        return await query
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Members.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(member => member.IsActive == isActive.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<Member?> GetByIdWithUserAsync(int memberId, bool asNoTracking, CancellationToken cancellationToken = default) =>
        CreateWithUserQuery(asNoTracking)
            .SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public Task<Member?> GetByCodeWithUserAsync(string normalizedCode, bool asNoTracking, CancellationToken cancellationToken = default) =>
        CreateWithUserQuery(asNoTracking)
            .SingleOrDefaultAsync(member => member.MembershipCode == normalizedCode, cancellationToken);

    public Task<Member?> GetByIdAsync(int memberId, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Members.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);
    }

    public Task<Member?> GetTrackedByIdAsync(int memberId, CancellationToken cancellationToken = default) =>
        _dbContext.Members
            .SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public Task<Member?> GetTrackedByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) =>
        _dbContext.Members
            .SingleOrDefaultAsync(member => member.MembershipCode == normalizedCode, cancellationToken);

    public Task<bool> ExistsAsync(int memberId, CancellationToken cancellationToken = default) =>
        _dbContext.Members
            .AsNoTracking()
            .AnyAsync(member => member.Id == memberId, cancellationToken);

    public Task<string?> GetMembershipCodeAsync(int memberId, CancellationToken cancellationToken = default) =>
        _dbContext.Members
            .AsNoTracking()
            .Where(member => member.Id == memberId)
            .Select(member => member.MembershipCode)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetExistingMembershipCodesForYearPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        await _dbContext.Members
            .AsNoTracking()
            .Where(member => member.MembershipCode.StartsWith(prefix))
            .Select(member => member.MembershipCode)
            .ToListAsync(cancellationToken);

    public void Add(Member member) =>
        _dbContext.Members.Add(member);

    private IQueryable<Member> CreateWithUserQuery(bool asNoTracking)
    {
        var query = _dbContext.Members
            .Include(member => member.User)
            .AsQueryable();

        return asNoTracking ? query.AsNoTracking() : query;
    }
}
