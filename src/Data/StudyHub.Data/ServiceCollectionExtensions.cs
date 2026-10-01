using Microsoft.Extensions.DependencyInjection;
using StudyHub.Data.Courses;
using StudyHub.Data.Documents;
using StudyHub.Data.Notes;
using StudyHub.Data.Semesters;
using StudyHub.Logic.Domain.Contract;

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
