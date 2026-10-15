using Chugim.Domain.Enums;

namespace Chugim.Application.Common;

// המשתמשת המחוברת, כפי שנקראה מה-token בשכבת ה-API.
// מועברת ל-services כפרמטר — כך הם לא תלויים ב-HttpContext וקל לבדוק אותם.
public sealed record CurrentUser(int Id, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
}
