using Microsoft.Extensions.DependencyInjection;
using StudyHub.Infrastructure.Courses;
using StudyHub.Infrastructure.Notes;
using StudyHub.Infrastructure.Semesters;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Notes;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ISemesterRepository, SemesterRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}
