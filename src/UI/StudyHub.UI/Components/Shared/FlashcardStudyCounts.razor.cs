using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Flashcards;

namespace StudyHub.UI.Components.Shared;

/// <summary>Today's new / learning / review counts in Anki's colors (blue / red / green).</summary>
public partial class FlashcardStudyCounts
{
    [Parameter]
    [EditorRequired]
    public FlashcardStudyCountsDto Counts { get; set; } = FlashcardStudyCountsDto.Empty;
}
