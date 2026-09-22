using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Business.Contract.Courses;
using StudyHub.Logic.Business.Contract.Dashboard;
using StudyHub.Logic.Business.Contract.Documents;
using StudyHub.Logic.Business.Contract.Notes;
using StudyHub.Logic.Business.Contract.Semesters;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Documents;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Contract.SemesterProgress;
using StudyHub.Logic.Domain.Contract.Semesters;
using StudyHub.Logic.Domain.SemesterProgress;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Logic.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubBusiness(this IServiceCollection services)
    {
        services.AddScoped<ICourseOrchestrator, CourseOrchestrator>();
        services.AddScoped<ISemesterOrchestrator, SemesterOrchestrator>();
        services.AddScoped<IDashboardOrchestrator, DashboardOrchestrator>();
        services.AddScoped<IDocumentOrchestrator, DocumentOrchestrator>();
        services.AddScoped<INoteOrchestrator, NoteOrchestrator>();
        services.AddScoped<ISemesterProgressCalculator, SemesterProgressCalculator>();
        services.AddScoped<IActiveSemesterProvider, ActiveSemesterProvider>();

        return services;
    }
}
