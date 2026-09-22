using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Documents;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Logic.Integration.Semesters;

namespace StudyHub.Logic.Integration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseAddress = new Uri(configuration["Api:BaseAddress"]!);

        services.AddHttpClient<ISemesterAccessor, SemesterAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<ICourseAccessor, CourseAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<IDocumentAccessor, DocumentAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<IDashboardAccessor, DashboardAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<INoteAccessor, NoteAccessor>(client => client.BaseAddress = apiBaseAddress);

        return services;
    }
}
