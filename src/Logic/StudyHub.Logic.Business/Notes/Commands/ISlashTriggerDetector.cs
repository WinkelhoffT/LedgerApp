namespace StudyHub.Logic.Business.Notes.Commands;

public interface ISlashTriggerDetector
{
    /// <summary>
    /// Looks backwards from <paramref name="caretIndex"/> for an active "/query" the caret is
    /// currently inside of, or <c>null</c> if there isn't one.
    /// </summary>
    SlashTrigger? Detect(string text, int caretIndex);
}
