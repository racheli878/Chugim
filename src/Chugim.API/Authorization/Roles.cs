using Chugim.Domain.Enums;

namespace Chugim.API.Authorization;

// שמות ה-roles כקבועים, לשימוש ב-[Authorize(Roles = ...)]. נגזרים מה-enum, כך שאין מחרוזות כפולות.
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Participant = nameof(UserRole.Participant);
}
