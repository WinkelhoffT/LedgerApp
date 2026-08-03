namespace StudyHub.Logic.Business.Notes.Commands;

public interface ISlashCommandRegistry
{
    /// <summary>
    /// Returns commands whose title or keywords match <paramref name="query"/>, in a stable
    /// category order. An empty query returns every command.
    /// </summary>
    IReadOnlyList<ISlashCommand> Search(string query);
}
