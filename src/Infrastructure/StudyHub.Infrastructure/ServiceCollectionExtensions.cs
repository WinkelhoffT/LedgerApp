using Microsoft.Extensions.DependencyInjection;
using StudyHub.Infrastructure.Courses;
using StudyHub.Infrastructure.Documents;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;

namespace StudyHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    // Course/Document repositories still live here pending their own migration to StudyHub.Data
    // (see review.md gap 1.7); Semester's repository has already moved to StudyHub.Data.
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }
}
