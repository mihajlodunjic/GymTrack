using System.Security.Claims;
using GymTrack.Common.Exceptions;
using GymTrack.DTOs.Auth;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using GymTrack.Security;
using GymTrack.Services;
using MediatR;

namespace GymTrack.Application.Auth;

public sealed record LoginCommand(LoginRequest Request) : IRequest<LoginResponse>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(command.Request.Email);
        var user = await _userRepository.GetByEmailWithMemberAsync(normalizedEmail, cancellationToken);

        if (user is null || !_passwordService.VerifyPassword(command.Request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        var tokenResult = _tokenService.GenerateToken(user);

        return new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresAt = tokenResult.ExpiresAt,
            User = MapCurrentUser(user)
        };
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static CurrentUserResponse MapCurrentUser(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            MemberId = user.Member?.Id
        };
}

public sealed record GetCurrentUserQuery(ClaimsPrincipal Principal) : IRequest<CurrentUserResponse>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var userId = query.Principal.GetRequiredUserId();
        var user = await _userRepository.GetByIdWithMemberAsync(userId, true, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Current user could not be resolved.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("User account is inactive.");
        }

        return new CurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            MemberId = user.Member?.Id
        };
    }
}
