namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class TaskListSlashCommand : ISlashCommand
{
    public string Id => "task-list";

    public string Title => "Task List";

    public string Icon => "☐";

    public SlashCommandCategory Category => SlashCommandCategory.Lists;

    public IReadOnlyList<string> Keywords { get; } = ["task", "todo", "checkbox", "checklist"];

    public SlashCommandInsertion Apply(string precedingText)
    {
        const string text = "- [ ] ";
        return new SlashCommandInsertion(text, text.Length);
    }
}
