using Microsoft.Extensions.DependencyInjection;
using StudyHub.Data.Courses;
using StudyHub.Data.Documents;
using StudyHub.Data.Semesters;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubDataRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISemesterRepository, SemesterRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }
}
