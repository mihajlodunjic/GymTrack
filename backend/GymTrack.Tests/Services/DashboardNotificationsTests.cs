using System.Reflection;
using GymTrack.Application.Dashboard;
using GymTrack.Controllers;
using GymTrack.Entities;
using GymTrack.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GymTrack.Tests.Services;

public sealed class DashboardNotificationsTests
{
    [Fact]
    public async Task GetRecentNotificationsAsync_ReturnsNotificationsInDescendingOrder()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.SystemNotifications.AddRange(
            new SystemNotification
            {
                Title = "Older",
                Message = "Older notification",
                Type = SystemNotificationType.Info,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new SystemNotification
            {
                Title = "Newer",
                Message = "Newer notification",
                Type = SystemNotificationType.Report,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();
        var notifications = await mediator.Send(new GetRecentNotificationsQuery());

        Assert.Equal(2, notifications.Count);
        Assert.Equal("Newer", notifications[0].Title);
        Assert.Equal("Older", notifications[1].Title);
    }

    [Fact]
    public void DashboardController_RequiresAdminRole()
    {
        var authorizeAttribute = typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(nameof(UserRole.Admin), authorizeAttribute!.Roles);
    }
}
