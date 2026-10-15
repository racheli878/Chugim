using System.ComponentModel.DataAnnotations;

namespace Chugim.Application.DTOs.Auth;

// הרשמה עצמית יוצרת תמיד משתתפת. מנהלות נוצרות ב-seed בלבד.
public sealed record RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
