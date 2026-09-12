using StudyHub.UI.Components;
using StudyHub.UI.Courses;
using StudyHub.UI.Dashboard;
using StudyHub.UI.Documents;
using StudyHub.UI.Semesters;
using StudyHub.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IPageHeaderService, PageHeaderService>();
builder.Services.AddScoped<ISidebarStateService, SidebarStateService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

builder.Services.AddHttpClient<ICourseAccessor, CourseAccessor>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<ISemesterAccessor, SemesterAccessor>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<IDashboardAccessor, DashboardAccessor>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]!));

builder.Services.AddHttpClient<IDocumentAccessor, DocumentAccessor>(client =>
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
app.MapDocumentDownloadEndpoints();

app.Run();
