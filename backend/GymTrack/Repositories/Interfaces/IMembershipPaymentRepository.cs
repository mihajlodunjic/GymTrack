using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface IMembershipPaymentRepository
{
    Task<IReadOnlyList<MembershipPayment>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<MembershipPayment?> GetByIdWithDetailsAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPayment>> GetForMemberWithDetailsAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPayment>> GetForMemberTrackedWithDetailsAsync(int memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPayment>> GetForMemberForActiveSelectionAsync(int memberId, CancellationToken cancellationToken = default);

    void Add(MembershipPayment payment);
}
