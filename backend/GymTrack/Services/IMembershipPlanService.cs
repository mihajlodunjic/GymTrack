using GymTrack.DTOs.MembershipPlan;

namespace GymTrack.Services;

public interface IMembershipPlanService
{
    Task<IReadOnlyList<MembershipPlanResponse>> GetAllPlansAsync(CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> GetPlanByIdAsync(int planId, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> CreateTimeBasedPlanAsync(CreateTimeBasedPlanRequest request, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> CreateVisitBasedPlanAsync(CreateVisitBasedPlanRequest request, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> CreateCombinedPlanAsync(CreateCombinedPlanRequest request, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> UpdateTimeBasedPlanAsync(int planId, UpdateTimeBasedPlanRequest request, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> UpdateVisitBasedPlanAsync(int planId, UpdateVisitBasedPlanRequest request, CancellationToken cancellationToken = default);

    Task<MembershipPlanResponse> UpdateCombinedPlanAsync(int planId, UpdateCombinedPlanRequest request, CancellationToken cancellationToken = default);

    Task DeactivatePlanAsync(int planId, CancellationToken cancellationToken = default);
}
