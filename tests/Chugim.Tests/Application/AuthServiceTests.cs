using Chugim.Application.Common;
using Chugim.Application.DTOs.Auth;
using Chugim.Application.Interfaces;
using Chugim.Application.Services;
using Chugim.Domain.Entities;
using Chugim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Chugim.Tests.TestSupport.TestData;

namespace Chugim.Tests.Application;

public class AuthServiceTests
{
    private const string Password = "Demo1234!";
    private const string PasswordHash = "hashed-password";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_users.Object, _unitOfWork.Object, _passwordHasher.Object,
            _tokenGenerator.Object, Mapper, NullLogger<AuthService>.Instance);

        _passwordHasher.Setup(h => h.Hash(Password)).Returns(PasswordHash);
        _tokenGenerator.Setup(g => g.Generate(It.IsAny<User>())).Returns(new AccessToken("jwt-token", Now));
    }

    [Fact]
    public async Task Register_NewEmail_CreatesParticipantWithHashedPasswordAndReturnsToken()
    {
        User? added = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => added = u);
        var request = new RegisterRequest { FullName = " רחל כהן ", Email = " Rachel@Example.COM ", Password = Password };

        var result = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.Token);
        Assert.NotNull(added);
        Assert.Equal("rachel@example.com", added.Email);   // מייל מנורמל
        Assert.Equal("רחל כהן", added.FullName);
        Assert.Equal(PasswordHash, added.PasswordHash);    // נשמר hash, לא הסיסמה
        Assert.Equal(UserRole.Participant, added.Role);    // הרשמה עצמית — תמיד משתתפת
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflictAndDoesNotSave()
    {
        _users.Setup(r => r.EmailExistsAsync("rachel@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new RegisterRequest { FullName = "רחל", Email = "rachel@example.com", Password = Password };

        var result = await _service.RegisterAsync(request, CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        SetupExistingUser();
        _passwordHasher.Setup(h => h.Verify(PasswordHash, Password)).Returns(true);

        var result = await _service.LoginAsync(new LoginRequest { Email = "sara@chugim.dev", Password = Password }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.Token);
        Assert.Equal(UserRole.Participant, result.Value.Role);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_ReturnTheSameError()
    {
        SetupExistingUser();
        _passwordHasher.Setup(h => h.Verify(PasswordHash, It.IsAny<string>())).Returns(false);

        var wrongPassword = await _service.LoginAsync(
            new LoginRequest { Email = "sara@chugim.dev", Password = "wrong" }, CancellationToken.None);
        var unknownEmail = await _service.LoginAsync(
            new LoginRequest { Email = "nobody@chugim.dev", Password = Password }, CancellationToken.None);

        // אותה תשובה בדיוק — כדי שאי אפשר יהיה לגלות אילו מיילים רשומים.
        Assert.Equal(ErrorType.Unauthorized, wrongPassword.Error!.Type);
        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
        _tokenGenerator.Verify(g => g.Generate(It.IsAny<User>()), Times.Never);
    }

    private void SetupExistingUser() =>
        _users.Setup(r => r.GetByEmailAsync("sara@chugim.dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = 4, FullName = "שרה מזרחי", Email = "sara@chugim.dev",
                PasswordHash = PasswordHash, Role = UserRole.Participant
            });
}
