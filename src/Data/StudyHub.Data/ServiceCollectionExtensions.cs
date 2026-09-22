using Microsoft.Extensions.DependencyInjection;
using StudyHub.Data.Courses;
using StudyHub.Data.Documents;
using StudyHub.Data.Notes;
using StudyHub.Data.Semesters;
using StudyHub.Data.Contract.Courses;
using StudyHub.Data.Contract.Documents;
using StudyHub.Data.Contract.Notes;
using StudyHub.Data.Contract.Semesters;

namespace StudyHub.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubDataRepositories(this IServiceCollection services)
    {
        services.AddScoped<ISemesterRepository, SemesterRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}
