using StudyHub.Shared.Flashcards;

namespace StudyHub.UI.Flashcards;

/// <summary>Mutable page-state copy of a <see cref="FlashcardDto"/> for inline editing.</summary>
public sealed class EditableFlashcard
{
    public string Front { get; set; } = string.Empty;

    public string Back { get; set; } = string.Empty;

    /// <summary>Comma-separated, like note tags.</summary>
    public string TagsText { get; set; } = string.Empty;

    public bool IsEditing { get; set; }

    public static EditableFlashcard FromDto(FlashcardDto card) =>
        new() { Front = card.Front, Back = card.Back, TagsText = string.Join(", ", card.Tags) };

    public FlashcardDto ToDto() =>
        new(
            Front,
            Back,
            TagsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
