using AutoMapper;
using Chugim.Application.Common;
using Chugim.Application.DTOs.Auth;
using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Chugim.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await _users.EmailExistsAsync(email, cancellationToken))
            return Error.Conflict("כתובת המייל כבר רשומה במערכת.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Participant
        };

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // לא כותבים ללוג מייל, סיסמה או token — רק מזהה.
        _logger.LogInformation("User {UserId} registered", user.Id);
        return CreateAuthResponse(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        // אותה תשובה למייל לא קיים ולסיסמה שגויה — כדי לא לחשוף אילו מיילים רשומים.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogWarning("Failed login attempt");
            return Error.Unauthorized("מייל או סיסמה שגויים.");
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokenGenerator.Generate(user);
        return _mapper.Map<AuthResponse>(user) with
        {
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
