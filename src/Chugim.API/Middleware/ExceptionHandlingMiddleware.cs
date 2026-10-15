using Microsoft.AspNetCore.Mvc;

namespace Chugim.API.Middleware;

/// <summary>
/// הרשת האחרונה: כל חריגה שלא טופלה נרשמת ללוג ברמת Error, והלקוח מקבל 500
/// בפורמט ProblemDetails האחיד — בלי stack trace ובלי פרטים פנימיים.
/// שגיאות צפויות (404, 409 וכו') לא מגיעות לכאן — הן חוזרות מה-services כ-Result.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IProblemDetailsService problemDetails)
    {
        _next = next;
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // הלקוח ניתק באמצע — זו לא תקלה בשרת, ואין למי להחזיר תשובה.
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            // middleware זה רץ לפני CorrelationIdMiddleware, ולכן פותח scope משלו עם אותו מזהה.
            using (_logger.BeginScope(new Dictionary<string, object>
                   { [CorrelationIdMiddleware.LogScopeKey] = context.TraceIdentifier }))
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await _problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "אירעה שגיאה בלתי צפויה.",
                    Detail = "נסי שוב מאוחר יותר. אם הבעיה חוזרת, צייני את מזהה הבקשה (correlationId)."
                }
            });
        }
    }
}
