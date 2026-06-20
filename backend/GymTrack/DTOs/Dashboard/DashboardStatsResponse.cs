namespace GymTrack.DTOs.Dashboard;

public sealed class DashboardStatsResponse
{
    public int TotalMembers { get; init; }

    public int ActiveMembers { get; init; }

    public int InactiveMembers { get; init; }

    public int ActiveMemberships { get; init; }

    public int ExpiredMemberships { get; init; }

    public int TodayCheckIns { get; init; }

    public int CurrentMonthPayments { get; init; }

    public decimal CurrentMonthRevenue { get; init; }

    public int ExpiringInNextSevenDays { get; init; }
}
