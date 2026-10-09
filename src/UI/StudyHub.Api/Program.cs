using Microsoft.EntityFrameworkCore;
using StudyHub.Api;
using StudyHub.Api.CalendarEvents;
using StudyHub.Api.Courses;
using StudyHub.Api.Documents;
using StudyHub.Api.Flashcards;
using StudyHub.Api.Notes;
using StudyHub.Api.Semesters;
using StudyHub.Api.StudySessions;
using StudyHub.Data;
using StudyHub.Infrastructure;
using StudyHub.Logic.Business;
using StudyHub.Logic.Integration.Ai;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStudyHubData(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddStudyHubDataRepositories();
builder.Services.AddStudyHubInfrastructure();
builder.Services.AddStudyHubBusiness();
builder.Services.AddStudyHubFlashcardStudy(builder.Configuration);
builder.Services.AddStudyHubCalendar(builder.Configuration);
builder.Services.AddStudyHubAi(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddExceptionHandler<CourseExceptionHandler>();
builder.Services.AddExceptionHandler<SemesterExceptionHandler>();
builder.Services.AddExceptionHandler<DocumentExceptionHandler>();
builder.Services.AddExceptionHandler<NoteExceptionHandler>();
builder.Services.AddExceptionHandler<FlashcardExceptionHandler>();
builder.Services.AddExceptionHandler<StudySessionExceptionHandler>();
builder.Services.AddExceptionHandler<CalendarEventExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Guarded by IsRelational() so tests can swap in UseInMemoryDatabase (which doesn't support migrations).
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

namespace StudyHub.Api
{
    public partial class Program;
}
