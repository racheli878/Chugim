using Chugim.Application.Mappings;
using Chugim.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Chugim.Application;

public static class DependencyInjection
{
    /// <summary>
    /// רושם את כל שירותי שכבת Application. נקרא פעם אחת מ-Program.cs.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped: ה-services תלויים ב-repositories וב-DbContext, שהם Scoped (מופע לכל בקשה).
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();

        // סורק את ה-assembly ומוצא את כל ה-Profiles (כרגע MappingProfile).
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        return services;
    }
}
