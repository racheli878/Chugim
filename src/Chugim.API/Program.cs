using System.Text.Json.Serialization;
using Chugim.API.Extensions;
using Chugim.API.Middleware;
using Chugim.Application;
using Chugim.Infrastructure;
using NLog;
using NLog.Web;

// לוגר שזמין עוד לפני שהשרת בנוי — כדי שגם כשל בעלייה (מסד לא זמין, מפתח JWT חסר) יירשם.
var startupLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------- Logging ----------
    // NLog מחליף את ספקי הלוג המובנים; הקוד ממשיך להשתמש ב-ILogger<T>.
    // RemoveLoggerFactoryFilter = false: רמות הלוג נקבעות ב-appsettings, לא ב-nlog.config.
    builder.Logging.ClearProviders();
    builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

    // ---------- Services ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddJwtAuthentication();
    builder.Services.AddAuthRateLimiting();
    builder.Services.AddClientCors(builder.Configuration);
    builder.Services.AddProxySupport();

    // פורמט שגיאה אחיד (ProblemDetails) לכל התשובות, עם מזהה הבקשה.
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier);

    // enums כמחרוזות ב-JSON ("Sunday", "Active", "Admin").
    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddSwaggerWithJwt();

    var app = builder.Build();

    await app.Services.InitializeDatabaseAsync();

    // ---------- Pipeline (הסדר קובע) ----------
    app.UseMiddleware<ExceptionHandlingMiddleware>(); // 1. עוטף את כל השאר
    app.UseForwardedHeaders();                         // 2. IP ו-https אמיתיים מאחורי proxy
    app.UseMiddleware<CorrelationIdMiddleware>();      // 3. מזהה + לוג לכל בקשה
    app.UseStatusCodePages();                          // 4. גוף ProblemDetails גם ל-401/403/429 ריקים

    // Swagger פעיל גם בייצור — כדי שאפשר יהיה לבדוק את השרת שפורסם.
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.UseCors(WebServiceExtensions.ClientCorsPolicy); // לפני האימות: גם בקשת OPTIONS של הדפדפן צריכה לעבור
    app.UseAuthentication();                            // "מי את?"
    app.UseAuthorization();                             // "מה מותר לך?"
    app.UseRateLimiter();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // HostAbortedException: עצירה רגילה ע"י dotnet ef
{
    startupLogger.Error(ex, "Application stopped because of an unhandled exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
