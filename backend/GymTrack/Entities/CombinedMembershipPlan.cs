using GymTrack.Enums;

namespace GymTrack.Entities;

public sealed class CombinedMembershipPlan : MembershipPlan
{
    public CombinedMembershipPlan()
    {
        PlanType = MembershipPlanType.Combined;
    }
}
