using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Services;

namespace GymTrack.Tests.Services;

public sealed class QrCodeServiceTests
{
    [Fact]
    public async Task GenerateQrCodeForMemberAsync_UsesMembershipCode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var member = await SeedMemberAsync(dbContext, "mika@example.com", "GYM-2026-0007");
        var service = new QrCodeService(dbContext);

        var fromMember = await service.GenerateQrCodeForMemberAsync(member.Id);
        var fromText = service.GenerateQrCodeFromText(member.MembershipCode);

        Assert.NotEmpty(fromMember);
        Assert.Equal(fromText, fromMember);
    }

    [Fact]
    public void GenerateQrCodeFromText_RejectsBlankText()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new QrCodeService(dbContext);

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
