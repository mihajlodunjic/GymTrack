using System.Security.Claims;
using GymTrack.DTOs.MembershipPayment;

namespace GymTrack.Services;

public interface IMembershipPaymentService
{
    Task<IReadOnlyList<MembershipPaymentResponse>> GetAllPaymentsAsync(CancellationToken cancellationToken = default);

    Task<MembershipPaymentResponse> GetPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<MembershipPaymentResponse> CreatePaymentAsync(
        CreateMembershipPaymentRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPaymentResponse>> GetPaymentsForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPaymentResponse>> GetCurrentMemberPaymentsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<MembershipPaymentResponse?> GetActiveMembershipForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    Task<MembershipStatusResponse> GetMembershipStatusForMemberAsync(int memberId, CancellationToken cancellationToken = default);

    Task<MembershipStatusResponse> GetCurrentMemberStatusAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<MembershipStatusResponse> GetMembershipStatusByCodeAsync(string membershipCode, CancellationToken cancellationToken = default);
}
