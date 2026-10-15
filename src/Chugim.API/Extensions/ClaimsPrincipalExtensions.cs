using System.Security.Claims;
using Chugim.Application.Common;
using Chugim.Domain.Enums;

namespace Chugim.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// בונה את CurrentUser מה-claims של ה-token שכבר אומת. נקרא רק ב-endpoints עם [Authorize].
    /// </summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var id = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Enum.Parse<UserRole>(principal.FindFirstValue(ClaimTypes.Role)!);
        return new CurrentUser(id, role);
    }
}
