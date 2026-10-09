using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.StudySessions;
using StudyHub.Shared.StudySessions;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// A session with its time, title, course or semester and location, as in the day panel and on the
/// Dashboard. A session of today or earlier can be marked as done with its planned duration.
/// </summary>
public partial class StudySessionCard
{
    [Inject]
    private IStudySessionAccessor SessionAccessor { get; set; } = default!;

    [Parameter, EditorRequired]
    public StudySessionDto Session { get; set; } = default!;

    /// <summary>Sessions before this date are shown dimmed; sessions after it cannot be marked as done.</summary>
    [Parameter]
    public DateOnly? Today { get; set; }

    /// <summary>Makes the card a button that reports the session when clicked.</summary>
    [Parameter]
    public EventCallback<StudySessionDto> OnSelected { get; set; }

    /// <summary>Shows the "Mark as done" button and reports the session once it is done.</summary>
    [Parameter]
    public EventCallback<StudySessionDto> OnCompleted { get; set; }

    private bool IsCompleting { get; set; }

    private string? ErrorMessage { get; set; }

    private string PastClass => Session.Date < Today ? "is-past" : string.Empty;

    private bool CanComplete => OnCompleted.HasDelegate && Session.Date <= Today;

    private async Task CompleteAsync()
    {
        IsCompleting = true;
        ErrorMessage = null;

        try
        {
            var completed = await SessionAccessor.CompleteAsync(
                Session.Id,
                new CompleteStudySessionRequest(Session.DurationMinutes)
            );
            await OnCompleted.InvokeAsync(completed);
        }
        catch (Exception ex)
            when (ex is StudySessionValidationException or StudySessionNotFoundException)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsCompleting = false;
        }
    }
}
