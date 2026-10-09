using Microsoft.AspNetCore.Components;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.UI.Components.Shared;

/// <summary>One task on the exam sheet: radio options for single choice, a text area for an open task.</summary>
public partial class PracticeExamSheetTask
{
    [Parameter, EditorRequired]
    public PracticeExamSheetTaskDto ExamTask { get; set; } = default!;

    [Parameter]
    public Guid? SelectedOptionId { get; set; }

    [Parameter]
    public string? AnswerText { get; set; }

    [Parameter]
    public bool IsReadOnly { get; set; }

    [Parameter]
    public EventCallback<Guid> OnOptionSelected { get; set; }

    /// <summary>Raised on every keystroke, so the page can save after a pause.</summary>
    [Parameter]
    public EventCallback<string> OnTextInput { get; set; }

    /// <summary>Raised when the text area loses focus after a change.</summary>
    [Parameter]
    public EventCallback OnTextCommitted { get; set; }

    private bool IsMonospace { get; set; }
}
