using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Business.Courses;

namespace StudyHub.Logic.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubBusiness(this IServiceCollection services)
    {
        services.AddScoped<ICourseManagement, CourseManagement>();

        return services;
    }
}
