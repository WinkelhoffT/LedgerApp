using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Shared.Flashcards;

namespace StudyHub.UI.Components.Shared;

public partial class FlashcardCardDialog
{
    private static readonly char[] TagSeparators = [' ', ',', '\t', '\n', '\r'];

    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public Guid DeckId { get; set; }

    [Parameter]
    public DeckCardDto? EditingCard { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnSaved { get; set; }

    private string Front { get; set; } = string.Empty;

    private string Back { get; set; } = string.Empty;

    private string TagsText { get; set; } = string.Empty;

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private DeckCardDto? _lastLoadedCard;

    private bool _loaded;

    protected override void OnParametersSet()
    {
        if (!IsOpen || (_loaded && EditingCard == _lastLoadedCard))
        {
            return;
        }

        _lastLoadedCard = EditingCard;
        _loaded = true;
        ErrorMessage = null;
        Front = EditingCard?.Front ?? string.Empty;
        Back = EditingCard?.Back ?? string.Empty;
        TagsText = string.Join(' ', EditingCard?.Tags ?? []);
    }

    private async Task SubmitAsync()
    {
        IsSaving = true;
        ErrorMessage = null;
        var card = new FlashcardDto(Front, Back, TagsText.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries));

        try
        {
            if (EditingCard is null)
            {
                await DeckAccessor.AddCardsAsync(new AddFlashcardsRequest(DeckId, [card], SourceNoteId: null));
            }
            else
            {
                await DeckAccessor.UpdateCardAsync(new UpdateFlashcardRequest(EditingCard.Id, card));
            }

            await OnSaved.InvokeAsync();
            await Close();
        }
        catch (Exception ex) when (ex is FlashcardValidationException or FlashcardDeckArchivedException or FlashcardNotFoundException)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task Close()
    {
        ErrorMessage = null;
        _lastLoadedCard = null;
        _loaded = false;
        await OnClose.InvokeAsync();
    }
}
