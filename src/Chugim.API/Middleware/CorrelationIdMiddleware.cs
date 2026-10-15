using System.Diagnostics;

namespace Chugim.API.Middleware;

/// <summary>
/// נותן לכל בקשה מזהה ייחודי, מכניס אותו לכל שורות הלוג של הבקשה (log scope)
/// ומחזיר אותו ללקוח ב-header — כך אפשר לשחזר בקשה שלמה מתוך לוג משותף.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string LogScopeKey = "CorrelationId"; // תואם ל-${scopeproperty:CorrelationId} ב-nlog.config
    private const int MaxLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // TraceIdentifier משמש גם את ExceptionHandlingMiddleware ואת תשובות השגיאה.
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [LogScopeKey] = correlationId }))
        {
            _logger.LogInformation("HTTP {Method} {Path} started", context.Request.Method, context.Request.Path);
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }

    // מזהה שהגיע מהלקוח מתקבל רק אם הוא קצר ומכיל אותיות, ספרות ומקפים — כדי שלא יוזרק תוכן ללוג.
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var isValid = incoming.Length is > 0 and <= MaxLength && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
        return isValid ? incoming : Guid.NewGuid().ToString("N");
    }
}
