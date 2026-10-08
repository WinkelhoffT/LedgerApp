using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using StudyHub.Logic.Integration.Flashcards;
using StudyHub.Shared.Flashcards;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class FlashcardStudy
{
    [Inject]
    private IFlashcardStudyAccessor StudyAccessor { get; set; } = default!;

    [Inject]
    private IFlashcardDeckAccessor DeckAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [Parameter]
    public Guid DeckId { get; set; }

    private ElementReference StudyArea { get; set; }

    private StudyCardDto? Card { get; set; }

    private string? DeckName { get; set; }

    private bool IsLoading { get; set; } = true;

    private bool IsAnswerShown { get; set; }

    private bool IsAnswering { get; set; }

    private bool HasError => ErrorMessage is not null;

    private string? ErrorMessage { get; set; }

    // Keyboard shortcuts only reach the study area while it has focus; clicking a button that is
    // then removed from the page would otherwise leave focus on the document body.
    private bool _focusPending;

    protected override async Task OnParametersSetAsync()
    {
        PageHeader.SetHeader("Study", "Spaced repetition");
        DeckName = null;
        await LoadNextAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPending)
        {
            _focusPending = false;
            await StudyArea.FocusAsync();
        }
    }

    private async Task LoadNextAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            DeckName ??= (await DeckAccessor.GetByIdAsync(DeckId)).Name;
            ShowCard(await StudyAccessor.GetNextAsync(DeckId));
        }
        catch (Exception ex) when (ex is FlashcardDeckNotFoundException or FlashcardDeckArchivedException)
        {
            Card = null;
            ErrorMessage = ex is FlashcardDeckArchivedException
                ? "This deck is archived. Restore it to study it again."
                : "This deck could not be found.";
        }
        catch (HttpRequestException)
        {
            Card = null;
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowCard(StudyCardDto? card)
    {
        Card = card;
        IsAnswerShown = false;
        _focusPending = true;
        PageHeader.SetHeader(DeckName ?? "Study", "Spaced repetition");
    }

    private void ShowAnswer()
    {
        IsAnswerShown = true;
        _focusPending = true;
    }

    private async Task AnswerAsync(FlashcardRating rating)
    {
        if (Card is null || IsAnswering)
        {
            return;
        }

        IsAnswering = true;
        ErrorMessage = null;

        try
        {
            ShowCard(await StudyAccessor.AnswerAsync(new AnswerFlashcardRequest(Card.CardId, rating)));
        }
        catch (FlashcardNotDueException)
        {
            // The card was answered in the meantime (e.g. in another tab): continue with the current queue.
            await LoadNextAsync();
        }
        catch (Exception ex) when (ex is FlashcardNotFoundException or FlashcardDeckArchivedException)
        {
            ErrorMessage = ex.Message;
            await LoadNextAsync();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "StudyHub.Api could not be reached. Try again in a moment.";
        }
        finally
        {
            IsAnswering = false;
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Card is null || e.Repeat || e.CtrlKey || e.AltKey || e.MetaKey)
        {
            return;
        }

        if (!IsAnswerShown)
        {
            if (e.Key == " " || e.Key == "Enter")
            {
                ShowAnswer();
            }

            return;
        }

        if (e.Key is "1" or "2" or "3" or "4")
        {
            await AnswerAsync((FlashcardRating)(e.Key[0] - '0'));
        }
    }
}
