using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Business.Dashboard;
using StudyHub.Logic.Business.Notes;
using StudyHub.Logic.Business.Notes.Commands;
using StudyHub.Logic.Business.Semesters;
using StudyHub.UI.Components;
using StudyHub.UI.Courses;
using StudyHub.UI.Dashboard;
using StudyHub.UI.Notes;
using StudyHub.UI.Semesters;
using StudyHub.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IPageHeaderService, PageHeaderService>();
builder.Services.AddScoped<ISidebarStateService, SidebarStateService>();

builder.Services.AddSingleton<IMarkdownBlockParser, MarkdownBlockParser>();
builder.Services.AddSingleton<ISlashTriggerDetector, SlashTriggerDetector>();
builder.Services.AddSingleton<ISlashCommandRegistry, SlashCommandRegistry>();
builder.Services.AddSingleton<ISlashCommand>(new HeadingSlashCommand(1));
builder.Services.AddSingleton<ISlashCommand>(new HeadingSlashCommand(2));
builder.Services.AddSingleton<ISlashCommand>(new HeadingSlashCommand(3));
builder.Services.AddSingleton<ISlashCommand, QuoteSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, DividerSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, BulletListSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, NumberedListSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, TaskListSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, CodeBlockSlashCommand>();
builder.Services.AddSingleton<ISlashCommand, TableSlashCommand>();

builder.Services.AddHttpClient<ICourseManagement, CourseApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<ISemesterManagement, SemesterApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<IDashboardManagement, DashboardApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<INoteManagement, NoteApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
