using Microsoft.EntityFrameworkCore;
using StudyHub.Api;
using StudyHub.Api.Courses;
using StudyHub.Data;
using StudyHub.Infrastructure;
using StudyHub.Logic.Business;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStudyHubData(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddStudyHubInfrastructure();
builder.Services.AddStudyHubBusiness();

builder.Services.AddExceptionHandler<CourseExceptionHandler>();
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
app.MapCourseEndpoints();

app.Run();

public partial class Program;
