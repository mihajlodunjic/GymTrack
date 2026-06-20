using GymTrack.Enums;

namespace GymTrack.Entities;

public sealed class VisitBasedMembershipPlan : MembershipPlan
{
    public VisitBasedMembershipPlan()
    {
        PlanType = MembershipPlanType.VisitBased;
    }
}
