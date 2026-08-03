namespace StudyHub.Logic.Business.Notes.Commands;

/// <summary>
/// Holds every registered <see cref="ISlashCommand"/> in a fixed, category-grouped order and
/// filters them by a search query.
/// </summary>
public sealed class SlashCommandRegistry(IEnumerable<ISlashCommand> commands) : ISlashCommandRegistry
{
    private readonly IReadOnlyList<ISlashCommand> _commands = commands.ToList();

    public IReadOnlyList<ISlashCommand> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands;
        }

        return _commands.Where(command => Matches(command, query.Trim())).ToList();
    }

    private static bool Matches(ISlashCommand command, string query)
    {
        var titleWords = command.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return titleWords.Concat(command.Keywords)
            .Any(word => word.StartsWith(query, StringComparison.OrdinalIgnoreCase));
    }
}
