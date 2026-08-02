using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.SemesterProgress;

namespace StudyHub.Logic.Business;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStudyHubBusiness(this IServiceCollection services)
    {
        services.AddScoped<ICourseManagement, CourseManagement>();
        services.AddScoped<ISemesterManagement, SemesterManagement>();
        services.AddScoped<IDashboardManagement, DashboardManagement>();
        services.AddScoped<INoteManagement, NoteManagement>();
        services.AddScoped<ISemesterProgressCalculator, SemesterProgressCalculator>();

        return services;
    }
}
