using Microsoft.Extensions.DependencyInjection;

namespace StudyHub.Infrastructure;

// All repositories (Semester, Course, Document) now live in StudyHub.Data - see review.md gap
// 1.7. This project is reserved for actual external-infrastructure concerns (file storage,
// email, AI provider adapters, etc.) that don't exist yet.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubInfrastructure(this IServiceCollection services) => services;
}
