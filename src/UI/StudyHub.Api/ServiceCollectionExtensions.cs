using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudyHub.Data;
using StudyHub.Shared.Configuration;

namespace StudyHub.Api;

public static class ServiceCollectionExtensions
{
    private const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddStudyHubData(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath
    )
    {
        var connectionString = ResolveConnectionString(configuration, contentRootPath);

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }

    /// <summary>
    /// Binds <see cref="FlashcardStudyOptions"/> and validates it at startup, so a misconfigured time
    /// zone fails fast instead of on the first study request.
    /// </summary>
    public static IServiceCollection AddStudyHubFlashcardStudy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<FlashcardStudyOptions>()
            .Bind(configuration.GetSection(FlashcardStudyOptions.SectionName))
            .Validate(
                options => options.DayStartHour is >= 0 and <= 23,
                "Flashcards:DayStartHour must be between 0 and 23."
            )
            .Validate(
                options => IsKnownTimeZone(options.TimeZone),
                "Flashcards:TimeZone must be a known IANA time zone id."
            )
            .ValidateOnStart();

        // The Domain's StudyDayProvider takes the plain options object, keeping Domain free of the Options package.
        services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<FlashcardStudyOptions>>().Value
        );

        return services;
    }

    /// <summary>
    /// Binds <see cref="CalendarOptions"/> and validates it at startup, so a misconfigured time zone
    /// fails fast instead of on the first calendar request.
    /// </summary>
    public static IServiceCollection AddStudyHubCalendar(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<CalendarOptions>()
            .Bind(configuration.GetSection(CalendarOptions.SectionName))
            .Validate(
                options => IsKnownTimeZone(options.TimeZone),
                "Calendar:TimeZone must be a known IANA time zone id."
            )
            .ValidateOnStart();

        // The Domain's CalendarPeriodProvider takes the plain options object, keeping Domain free of the Options package.
        services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<CalendarOptions>>().Value
        );

        return services;
    }

    private static bool IsKnownTimeZone(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId)
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    private static string ResolveConnectionString(
        IConfiguration configuration,
        string contentRootPath
    )
    {
        var connectionString =
            configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured."
            );

        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ConnectionString;
    }
}
