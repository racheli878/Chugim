using Microsoft.OpenApi.Models;

namespace Chugim.API.Extensions;

public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Swagger עם כפתור Authorize: מדביקים את ה-token שהתקבל מ-login, וכל הבקשות נשלחות איתו.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Chugim API", Version = "v1" });

            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "הדביקי את ה-token בלבד (בלי המילה Bearer).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
        });

        return services;
    }
}
