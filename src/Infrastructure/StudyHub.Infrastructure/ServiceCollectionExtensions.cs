using Microsoft.Extensions.DependencyInjection;
using StudyHub.Infrastructure.Notes;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Infrastructure;

// Semester/Course/Document repositories now live in StudyHub.Data - see review.md gap 1.7.
// TODO(migration): NoteRepository below is temporary and will move to StudyHub.Data in a
// follow-up commit to match that convention. This project is otherwise reserved for actual
// external-infrastructure concerns (file storage, email, AI provider adapters, etc.).
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}
