using Chugim.API.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

namespace Chugim.API.Extensions;

public static class WebServiceExtensions
{
    public const string ClientCorsPolicy = "Client";

    /// <summary>
    /// מתיר ללקוח (React) לפנות לשרת מהדפדפן — רק מהכתובות שב-Cors:AllowedOrigins.
    /// </summary>
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // כדי שהלקוח יוכל לקרוא את מזהה הבקשה מהתשובה (למשל להציג אותו בהודעת שגיאה).
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

        return services;
    }

    /// <summary>
    /// ב-Render השרת יושב מאחורי proxy שמקבל את ה-HTTPS. בלי זה השרת היה רואה את כתובת ה-proxy
    /// במקום כתובת המשתמשת (חשוב ל-rate limiting) ו-http במקום https.
    /// </summary>
    public static IServiceCollection AddProxySupport(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // כתובות ה-proxy של Render אינן קבועות; השרת נגיש רק דרכו.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
