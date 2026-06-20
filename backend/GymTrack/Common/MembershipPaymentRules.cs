using GymTrack.Entities;
using GymTrack.Enums;

namespace GymTrack.Common;

public static class MembershipPaymentRules
{
    public static MembershipPayment? SelectPreferredActivePayment(IEnumerable<MembershipPayment> payments, DateTime today)
    {
        var timeBasedPayment = payments
            .Where(payment => IsTimeBasedActive(payment, today))
            .OrderBy(payment => payment.ValidUntil ?? DateTime.MaxValue)
            .ThenBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .FirstOrDefault();

        if (timeBasedPayment is not null)
        {
            return timeBasedPayment;
        }

        var combinedPayment = payments
            .Where(payment => IsCombinedActive(payment, today))
            .OrderBy(payment => payment.ValidUntil ?? DateTime.MaxValue)
            .ThenBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .FirstOrDefault();

        if (combinedPayment is not null)
        {
            return combinedPayment;
        }

        return payments
            .Where(IsVisitBasedActive)
            .OrderBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .FirstOrDefault();
    }

    public static bool IsActive(MembershipPayment payment, DateTime today) =>
        IsTimeBasedActive(payment, today) ||
        IsVisitBasedActive(payment) ||
        IsCombinedActive(payment, today);

    public static bool IsTimeBasedActive(MembershipPayment payment, DateTime today) =>
        payment.MembershipPlan.PlanType == MembershipPlanType.TimeBased &&
        payment.ValidUntil.HasValue &&
        payment.ValidFrom.Date <= today &&
        payment.ValidUntil.Value.Date >= today;

    public static bool IsVisitBasedActive(MembershipPayment payment) =>
        payment.MembershipPlan.PlanType == MembershipPlanType.VisitBased &&
        payment.TotalVisits.HasValue &&
        payment.UsedVisits.HasValue &&
        payment.UsedVisits.Value < payment.TotalVisits.Value;

    public static bool IsCombinedActive(MembershipPayment payment, DateTime today) =>
        payment.MembershipPlan.PlanType == MembershipPlanType.Combined &&
        payment.ValidUntil.HasValue &&
        payment.TotalVisits.HasValue &&
        payment.UsedVisits.HasValue &&
        payment.ValidFrom.Date <= today &&
        payment.ValidUntil.Value.Date >= today &&
        payment.UsedVisits.Value < payment.TotalVisits.Value;

    public static bool IsExpired(MembershipPayment payment, DateTime today) =>
        payment.MembershipPlan.PlanType switch
        {
            MembershipPlanType.TimeBased => payment.ValidUntil.HasValue && payment.ValidUntil.Value.Date < today,
            MembershipPlanType.VisitBased => payment.TotalVisits.HasValue && payment.UsedVisits.HasValue && payment.UsedVisits.Value >= payment.TotalVisits.Value,
            MembershipPlanType.Combined =>
                (payment.ValidUntil.HasValue && payment.ValidUntil.Value.Date < today) ||
                (payment.TotalVisits.HasValue && payment.UsedVisits.HasValue && payment.UsedVisits.Value >= payment.TotalVisits.Value),
            _ => false
        };

    public static int? CalculateRemainingVisits(MembershipPayment payment) =>
        payment.TotalVisits.HasValue && payment.UsedVisits.HasValue
            ? Math.Max(payment.TotalVisits.Value - payment.UsedVisits.Value, 0)
            : null;
}
