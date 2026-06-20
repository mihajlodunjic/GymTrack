using System.Security.Claims;
using GymTrack.DTOs.Member;

namespace GymTrack.Services;

public interface IMemberService
{
    Task<IReadOnlyList<MemberResponse>> GetAllMembersAsync(string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);

    Task<MemberDetailsResponse> GetMemberByIdAsync(int memberId, CancellationToken cancellationToken = default);

    Task<MemberDetailsResponse> GetMemberByCodeAsync(string membershipCode, CancellationToken cancellationToken = default);

    Task<MemberDetailsResponse> GetCurrentMemberProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task<MemberDetailsResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default);

    Task<MemberDetailsResponse> UpdateMemberAsync(int memberId, UpdateMemberRequest request, CancellationToken cancellationToken = default);

    Task DeactivateMemberAsync(int memberId, CancellationToken cancellationToken = default);
}
