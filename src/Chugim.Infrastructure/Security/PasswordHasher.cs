using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Chugim.Infrastructure.Security;

/// <summary>
/// מתאם (adapter) למימוש של Microsoft: PBKDF2 עם salt אקראי לכל סיסמה,
/// כך שגם שתי סיסמאות זהות נשמרות כ-hash שונה.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    // IPasswordHasher<User> (הגנרי) הוא של Microsoft; IPasswordHasher (בלי <>) הוא הממשק שלנו.
    private readonly IPasswordHasher<User> _inner;

    public PasswordHasher(IPasswordHasher<User> inner) => _inner = inner;

    // המימוש של Microsoft לא משתמש בפרמטר user, ולכן מועבר null.
    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
