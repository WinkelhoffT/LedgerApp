using Microsoft.AspNetCore.Components;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Flashcards
{
    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    protected override void OnInitialized() => PageHeader.SetHeader("Flashcards", "Spaced repetition");
}
