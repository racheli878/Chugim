using Chugim.API.Extensions;
using Chugim.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chugim.API.Controllers;

/// <summary>
/// בסיס לכל ה-controllers. כאן — ורק כאן — Result של ה-services מתורגם לסטטוס HTTP.
/// [ApiController] בודק את ModelState אוטומטית ומחזיר 400 עם פירוט השדות לפני שהפעולה רצה.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>המשתמשת המחוברת, מתוך ה-token. זמין רק בפעולות עם [Authorize].</summary>
    protected CurrentUser CurrentUser => User.ToCurrentUser();

    /// <summary>הצלחה ללא תוכן → 204.</summary>
    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : ProblemFor(result.Error);

    /// <summary>הצלחה עם ערך → 200.</summary>
    protected ActionResult<T> FromResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
            return ProblemFor(result.Error);

        return Ok(result.Value);
    }

    /// <summary>יצירה → 201 עם כותרת Location שמצביעה על המשאב החדש.</summary>
    protected ActionResult<T> CreatedFromResult<T>(Result<T> result, string actionName, Func<T, object> routeValues)
    {
        if (!result.IsSuccess)
            return ProblemFor(result.Error);

        return CreatedAtAction(actionName, routeValues(result.Value), result.Value);
    }

    private ObjectResult ProblemFor(Error error) =>
        Problem(detail: error.Message, statusCode: ToStatusCode(error.Type));

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
