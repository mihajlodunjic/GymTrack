using System.Security.Claims;
using GymTrack.DTOs.CheckIn;

namespace GymTrack.Services;

public interface ICheckInService
{
    Task<CheckInResponse> CheckInByMemberIdAsync(
        int memberId,
        CreateCheckInByMemberIdRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<CheckInResponse> CheckInByMembershipCodeAsync(
        string membershipCode,
        CreateCheckInByCodeRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckInResponse>> GetAllCheckInsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckInResponse>> GetCheckInsForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CheckInResponse>> GetCurrentMemberCheckInsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
