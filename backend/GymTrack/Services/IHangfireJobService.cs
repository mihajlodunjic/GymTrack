namespace GymTrack.Services;

public interface IHangfireJobService
{
    Task CheckExpiringMembershipsAsync();

    Task CreateDailyAdminReportAsync();
}
