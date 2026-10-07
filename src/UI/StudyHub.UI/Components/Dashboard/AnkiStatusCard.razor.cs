using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Anki;
using StudyHub.Shared.Dashboard;

namespace StudyHub.UI.Components.Dashboard;

/// <summary>
/// Shows today's due Anki cards. Renders nothing when the Anki integration is disabled.
/// </summary>
public partial class AnkiStatusCard
{
    private const int MaxDecksShown = 5;

    /// <summary><c>null</c> while the status is still loading.</summary>
    [Parameter]
    public AnkiStudyStatusDto? Status { get; set; }

    private IReadOnlyList<AnkiDeckCountsDto> TopDecks =>
        Status?.Decks
            .Where(deck => deck.NewCount + deck.LearnCount + deck.ReviewCount > 0)
            .Take(MaxDecksShown)
            .ToList()
        ?? [];
}
