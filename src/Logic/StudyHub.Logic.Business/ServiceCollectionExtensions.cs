using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.AddScoped<IFlashcardDeckOrchestrator, FlashcardDeckOrchestrator>();
        services.AddScoped<IDeckCardOrchestrator, DeckCardOrchestrator>();
        services.AddScoped<IFlashcardStudyOrchestrator, FlashcardStudyOrchestrator>();
        services.AddScoped<IFlashcardTransferOrchestrator, FlashcardTransferOrchestrator>();
        services.AddScoped<IStudySessionOrchestrator, StudySessionOrchestrator>();
        services.AddScoped<ICalendarOrchestrator, CalendarOrchestrator>();
        services.AddScoped<ICalendarEventOrchestrator, CalendarEventOrchestrator>();
        services.AddScoped<ISemesterProgressCalculator, SemesterProgressCalculator>();
        services.AddScoped<IActiveSemesterProvider, ActiveSemesterProvider>();
        services.AddScoped<ISemesterLifecycle, SemesterLifecycle>();
        services.AddScoped<ICourseLifecycle, CourseLifecycle>();
        services.AddScoped<IDocumentLifecycle, DocumentLifecycle>();
        services.AddScoped<INoteLifecycle, NoteLifecycle>();
        services.AddScoped<IFlashcardValidator, FlashcardValidator>();
        services.AddScoped<IAnkiCsvSerializer, AnkiCsvSerializer>();
        services.AddScoped<IAnkiCsvParser, AnkiCsvParser>();
        services.AddScoped<IFlashcardImportProcessor, FlashcardImportProcessor>();
        services.AddScoped<IFlashcardDeckLifecycle, FlashcardDeckLifecycle>();
        services.AddScoped<IFlashcardLifecycle, FlashcardLifecycle>();
        services.AddScoped<IFlashcardReviewProcessor, FlashcardReviewProcessor>();
        services.AddScoped<IStudyQueueProvider, StudyQueueProvider>();
        services.AddScoped<IStudySessionLifecycle, StudySessionLifecycle>();
        services.AddScoped<ICalendarLaneProcessor, CalendarLaneProcessor>();
        services.AddScoped<ICalendarEventLifecycle, CalendarEventLifecycle>();

        // Needs a FlashcardStudyOptions instance, which the host binds from configuration.
        services.AddSingleton<IStudyDayProvider, StudyDayProvider>();

        // Needs a CalendarOptions instance, which the host binds from configuration.
        services.AddSingleton<ICalendarPeriodProvider, CalendarPeriodProvider>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
