using GymTrack.Application.QrCodes;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GymTrack.Tests.Services;

public sealed class QrCodeServiceTests
{
    [Fact]
    public async Task GenerateQrCodeForMemberAsync_UsesMembershipCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0007");
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();
        var qrCodeService = provider.GetRequiredService<IQrCodeService>();

        var fromMember = await mediator.Send(new GenerateQrCodeForMemberQuery(member.Id));
        var fromText = qrCodeService.GenerateQrCodeFromText(member.MembershipCode);

        Assert.NotEmpty(fromMember);
        Assert.Equal(fromText, fromMember);
    }

    [Fact]
    public void GenerateQrCodeFromText_RejectsBlankText()
    {
        var service = new QrCodeService();

        Assert.Throws<BadRequestException>(() => service.GenerateQrCodeFromText("   "));
    }

    private static async Task<Member> SeedMemberAsync(AppDbContext dbContext, string email, string membershipCode)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hashed-password",
            Role = UserRole.Member,
            IsActive = true
        };

        var member = new Member
        {
            User = user,
            FirstName = "Mika",
            LastName = "Mikic",
            MembershipCode = membershipCode,
            IsActive = true
        };

        user.Member = member;

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }
}
