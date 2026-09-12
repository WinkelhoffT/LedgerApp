using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;

namespace StudyHub.UI.Components.Pages;

public partial class Error
{
    [Inject]
    private IWebHostEnvironment Environment { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    private string? ExceptionText { get; set; }

    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;

        if (Environment.IsDevelopment())
        {
            var exceptionFeature = HttpContext?.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error is { } exception)
            {
                ExceptionText = $"{exceptionFeature.Path}\n\n{exception}";
            }
        }
    }
}
