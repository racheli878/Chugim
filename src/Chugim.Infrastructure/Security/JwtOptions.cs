using System.ComponentModel.DataAnnotations;

namespace Chugim.Infrastructure.Security;

/// <summary>
/// נטען מסעיף "Jwt" בקונפיגורציה. המפתח לא נשמר ב-repo — User Secrets בפיתוח, משתנה סביבה בייצור.
/// משמש גם ליצירת ה-token (כאן) וגם לאימות שלו (API).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // HMAC-SHA256 דורש מפתח של לפחות 256 ביט (32 תווים).
    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}
