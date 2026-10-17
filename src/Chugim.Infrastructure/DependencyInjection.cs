using Chugim.Application.Interfaces;
using Chugim.Domain.Entities;
using Chugim.Infrastructure.Persistence;
using Chugim.Infrastructure.Persistence.Repositories;
using Chugim.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chugim.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// רושם את כל שירותי Infrastructure. זה המקום היחיד ש-API מכיר מתוך Infrastructure.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. In development set it with 'dotnet user-secrets'.");

        // DbContext נרשם כ-Scoped (ברירת המחדל): מופע אחד לכל בקשה.
        // Normalize: מקבל גם את פורמט ה-URL ש-Render נותן (postgres://...).
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(PostgresConnectionString.Normalize(connectionString)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        // Singleton: חסרי מצב ולא תלויים בשום שירות Scoped — מופע אחד לכל חיי השרת מספיק.
        // IPasswordHasher<User> (גנרי) — המימוש של Microsoft; IPasswordHasher — הממשק שלנו, שעוטף אותו.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // הגדרות JWT נבדקות כבר בעליית השרת: מפתח חסר או קצר מדי = השרת לא עולה.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// מחיל migrations שטרם הוחלו — כך המערכת עולה מוכנה בלי התערבות ידנית במסד.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
