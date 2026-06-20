using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.Data;
using GymTrack.DTOs.Member;
using GymTrack.Entities;
using GymTrack.Enums;
using GymTrack.Security;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Services;

public sealed class MemberService : IMemberService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public MemberService(AppDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<IReadOnlyList<MemberResponse>> GetAllMembersAsync(string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Members
            .AsNoTracking()
            .Include(member => member.User)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(member => member.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();

            query = query.Where(member =>
                member.FirstName.ToLower().Contains(normalizedSearch) ||
                member.LastName.ToLower().Contains(normalizedSearch) ||
                member.User.Email.ToLower().Contains(normalizedSearch) ||
                (member.PhoneNumber != null && member.PhoneNumber.ToLower().Contains(normalizedSearch)) ||
                member.MembershipCode.ToLower().Contains(normalizedSearch));
        }

        var members = await query
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync(cancellationToken);

        return members.Select(MapMemberResponse).ToArray();
    }

    public async Task<MemberDetailsResponse> GetMemberByIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await FindMemberByIdAsync(memberId, cancellationToken);
        return MapMemberDetails(member);
    }

    public async Task<MemberDetailsResponse> GetMemberByCodeAsync(string membershipCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeMembershipCode(membershipCode);

        var member = await _dbContext.Members
            .AsNoTracking()
            .Include(entity => entity.User)
            .SingleOrDefaultAsync(entity => entity.MembershipCode == normalizedCode, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with membership code '{normalizedCode}' was not found.");
        }

        return MapMemberDetails(member);
    }

    public async Task<MemberDetailsResponse> GetCurrentMemberProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var memberId = principal.GetRequiredMemberId();
        var member = await FindMemberByIdAsync(memberId, cancellationToken);
        return MapMemberDetails(member);
    }

    public async Task<MemberDetailsResponse> CreateMemberAsync(CreateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await _dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");
        }

        var membershipCode = await GenerateMembershipCodeAsync(cancellationToken);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Role = UserRole.Member,
            IsActive = true
        };

        var member = new Member
        {
            User = user,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = NormalizeOptionalValue(request.PhoneNumber),
            MembershipCode = membershipCode,
            IsActive = true
        };

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapMemberDetails(member);
    }

    public async Task<MemberDetailsResponse> UpdateMemberAsync(int memberId, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await _dbContext.Members
            .Include(entity => entity.User)
            .SingleOrDefaultAsync(entity => entity.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);

        var emailTaken = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail && user.Id != member.UserId, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException($"A user with email '{normalizedEmail}' already exists.");
        }

        member.FirstName = request.FirstName.Trim();
        member.LastName = request.LastName.Trim();
        member.PhoneNumber = NormalizeOptionalValue(request.PhoneNumber);
        member.User.Email = normalizedEmail;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapMemberDetails(member);
    }

    public async Task DeactivateMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member = await _dbContext.Members
            .Include(entity => entity.User)
            .SingleOrDefaultAsync(entity => entity.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        member.IsActive = false;
        member.User.IsActive = false;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Member> FindMemberByIdAsync(int memberId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.Members
            .AsNoTracking()
            .Include(entity => entity.User)
            .SingleOrDefaultAsync(entity => entity.Id == memberId, cancellationToken);

        if (member is null)
        {
            throw new NotFoundException($"Member with id '{memberId}' was not found.");
        }

        return member;
    }

    private async Task<string> GenerateMembershipCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"GYM-{year}-";

        var existingCodes = await _dbContext.Members
            .AsNoTracking()
            .Where(member => member.MembershipCode.StartsWith(prefix))
            .Select(member => member.MembershipCode)
            .ToListAsync(cancellationToken);

        var nextNumber = existingCodes
            .Select(code => code[prefix.Length..])
            .Select(suffix => int.TryParse(suffix, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        string candidate;
        do
        {
            candidate = $"{prefix}{nextNumber:0000}";
            nextNumber++;
        }
        while (existingCodes.Contains(candidate, StringComparer.OrdinalIgnoreCase));

        return candidate;
    }

    private static MemberResponse MapMemberResponse(Member member) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.User.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipCode = member.MembershipCode,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };

    private static MemberDetailsResponse MapMemberDetails(Member member) =>
        new()
        {
            Id = member.Id,
            UserId = member.UserId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.User.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipCode = member.MembershipCode,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string NormalizeMembershipCode(string membershipCode) =>
        membershipCode.Trim().ToUpperInvariant();

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
