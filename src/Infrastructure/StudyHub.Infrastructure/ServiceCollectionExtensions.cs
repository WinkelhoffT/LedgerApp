using Microsoft.Extensions.DependencyInjection;
using StudyHub.Infrastructure.Documents;
using StudyHub.Logic.Domain.Documents;

namespace StudyHub.Infrastructure;

public static class ServiceCollectionExtensions
{
    // Document's repository still lives here pending its own migration to StudyHub.Data (see
    // review.md gap 1.7); Semester's and Course's repositories have already moved.
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }
}
