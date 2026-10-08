using Microsoft.Extensions.DependencyInjection;
using StudyHub.Data.Contract;
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
        services.AddScoped<IFlashcardDeckRepository, FlashcardDeckRepository>();
        services.AddScoped<IFlashcardRepository, FlashcardRepository>();
        services.AddScoped<IStudySessionRepository, StudySessionRepository>();

        return services;
    }
}
