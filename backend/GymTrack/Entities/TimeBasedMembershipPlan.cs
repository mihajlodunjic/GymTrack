using GymTrack.Enums;

namespace GymTrack.Entities;

public sealed class TimeBasedMembershipPlan : MembershipPlan
{
    public TimeBasedMembershipPlan()
    {
        PlanType = MembershipPlanType.TimeBased;
    }
}
