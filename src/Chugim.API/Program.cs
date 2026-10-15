using System.Text.Json.Serialization;
using Chugim.API.Extensions;
using Chugim.API.Middleware;
using Chugim.Application;
using Chugim.Infrastructure;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// NLog מחליף את ספקי הלוג המובנים. הקוד ממשיך להשתמש ב-ILogger<T> הרגיל.
// RemoveLoggerFactoryFilter = false: רמות הלוג נקבעות ב-appsettings, לא ב-nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication();
builder.Services.AddAuthRateLimiting();

// פורמט שגיאה אחיד (ProblemDetails) לכל התשובות — 400, 401, 403, 404, 409, 429, 500 —
// וכל אחת כוללת את מזהה הבקשה.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier);

// enums כמחרוזות ב-JSON ("Sunday", "Active", "Admin") — קריא ללקוח ויציב לשינויי סדר.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

// סדר ה-pipeline: טיפול בשגיאות ראשון, כדי לעטוף את כל מה שאחריו.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// תשובות ריקות (401/403/429 מה-framework) מקבלות גוף ProblemDetails.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// authentication ("מי את?") חייב לבוא לפני authorization ("מה מותר לך?").
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
