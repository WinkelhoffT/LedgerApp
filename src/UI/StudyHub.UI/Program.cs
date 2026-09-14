using StudyHub.Logic.Integration;
using StudyHub.UI.Components;
using StudyHub.UI.Documents;
using StudyHub.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IPageHeaderStateHolder, PageHeaderStateHolder>();
builder.Services.AddScoped<ISidebarStateHolder, SidebarStateHolder>();
builder.Services.AddScoped<IThemeAccessor, ThemeAccessor>();
builder.Services.AddScoped<IThemeStateHolder, ThemeStateHolder>();

builder.Services.AddStudyHubIntegration(builder.Configuration);

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
