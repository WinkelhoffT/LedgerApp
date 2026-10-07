using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;

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
        services.AddScoped<IFlashcardOrchestrator, FlashcardOrchestrator>();
        services.AddScoped<ISemesterProgressCalculator, SemesterProgressCalculator>();
        services.AddScoped<IAnkiDueCardsCalculator, AnkiDueCardsCalculator>();
        services.AddScoped<IActiveSemesterProvider, ActiveSemesterProvider>();
        services.AddScoped<ISemesterLifecycle, SemesterLifecycle>();
        services.AddScoped<ICourseLifecycle, CourseLifecycle>();
        services.AddScoped<IDocumentLifecycle, DocumentLifecycle>();
        services.AddScoped<INoteLifecycle, NoteLifecycle>();
        services.AddScoped<IFlashcardValidator, FlashcardValidator>();
        services.AddScoped<IAnkiCsvSerializer, AnkiCsvSerializer>();

        return services;
    }
}
