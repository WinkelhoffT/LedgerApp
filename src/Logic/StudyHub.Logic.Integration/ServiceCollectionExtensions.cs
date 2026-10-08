using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyHub.Logic.Integration.Calendar;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Dashboard;
using StudyHub.Logic.Integration.Documents;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Logic.Integration.StudySessions;

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
        services.AddHttpClient<IFlashcardDeckAccessor, FlashcardDeckAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<IFlashcardStudyAccessor, FlashcardStudyAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<IFlashcardTransferAccessor, FlashcardTransferAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<ICalendarAccessor, CalendarAccessor>(client => client.BaseAddress = apiBaseAddress);
        services.AddHttpClient<IStudySessionAccessor, StudySessionAccessor>(client => client.BaseAddress = apiBaseAddress);

        // Generation waits for Claude (typically 15-60 s, up to the SDK timeout plus one retry).
        services.AddHttpClient<IFlashcardAccessor, FlashcardAccessor>(client =>
        {
            client.BaseAddress = apiBaseAddress;
            client.Timeout = TimeSpan.FromMinutes(6);
        });

        return services;
    }
}
