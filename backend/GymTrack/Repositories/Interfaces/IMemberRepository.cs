using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface IMemberRepository
{
    Task<IReadOnlyList<Member>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);

    Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken = default);

    Task<Member?> GetByIdWithUserAsync(int memberId, bool asNoTracking, CancellationToken cancellationToken = default);

    Task<Member?> GetByCodeWithUserAsync(string normalizedCode, bool asNoTracking, CancellationToken cancellationToken = default);

    Task<Member?> GetByIdAsync(int memberId, bool asNoTracking, CancellationToken cancellationToken = default);

    Task<Member?> GetTrackedByIdAsync(int memberId, CancellationToken cancellationToken = default);

    Task<Member?> GetTrackedByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int memberId, CancellationToken cancellationToken = default);

    Task<string?> GetMembershipCodeAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetExistingMembershipCodesForYearPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    void Add(Member member);
}
