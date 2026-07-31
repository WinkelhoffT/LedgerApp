using Microsoft.Extensions.DependencyInjection;
using StudyHub.Infrastructure.Courses;
using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();

        return services;
    }
}
