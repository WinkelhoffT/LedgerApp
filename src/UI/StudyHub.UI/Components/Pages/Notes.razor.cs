using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using StudyHub.Logic.Integration.Contract.Courses;
using StudyHub.Logic.Integration.Contract.Notes;
using StudyHub.Logic.Integration.Contract.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;
using StudyHub.UI.Components.Shared;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Notes
{
    private enum AssignmentTarget { Course, Semester }

    private static readonly Regex HeadingPattern = new(@"<h([1-3]) id=""([^""]+)"">(.*?)</h\1>", RegexOptions.Compiled);
    private static readonly Regex TagStripPattern = new("<.*?>", RegexOptions.Compiled);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().UseAutoIdentifiers().Build();

    private static readonly Dictionary<string, string> Templates = new()
    {
        ["blank"] = "",
        ["lecture"] = "# Lecture Notes\n\n## Topic\n\n## Key Points\n\n- \n\n## Open Questions\n\n- \n",
        ["exam"] = "# Exam Prep\n\n## Concepts to Review\n\n- \n\n## Practice Questions\n\n1. \n\n## Summary\n\n",
    };

    private static readonly IReadOnlyList<SlashCommand> SlashCommands =
    [
        new("Heading 1", "Big section heading", "# ", ""),
        new("Heading 2", "Medium section heading", "## ", ""),
        new("Heading 3", "Small section heading", "### ", ""),
        new("Bulleted list", "Simple bullet list", "- ", ""),
        new("Numbered list", "List with numbering", "1. ", ""),
        new("Task list", "To-do list with checkboxes", "- [ ] ", ""),
        new("Quote", "Capture a quote", "> ", ""),
        new("Code block", "Plain code snippet, no highlighting", "```\n", "\n```"),
        new("Code: C#", "C# snippet with syntax highlighting", "```csharp\n", "\n```"),
        new("Code: Java", "Java snippet with syntax highlighting", "```java\n", "\n```"),
        new("Code: Python", "Python snippet with syntax highlighting", "```python\n", "\n```"),
        new("Code: JavaScript", "JavaScript snippet with syntax highlighting", "```javascript\n", "\n```"),
        new("Code: TypeScript", "TypeScript snippet with syntax highlighting", "```typescript\n", "\n```"),
        new("Code: SQL", "SQL snippet with syntax highlighting", "```sql\n", "\n```"),
        new("Code: Bash", "Shell snippet with syntax highlighting", "```bash\n", "\n```"),
        new("Code: JSON", "JSON snippet with syntax highlighting", "```json\n", "\n```"),
        new("Code: HTML", "HTML snippet with syntax highlighting", "```html\n", "\n```"),
        new("Code: CSS", "CSS snippet with syntax highlighting", "```css\n", "\n```"),
        new("Table", "Simple 2-column table", "| Header | Header |\n| --- | --- |\n| Cell | Cell |\n", ""),
        new("Divider", "Horizontal rule", "\n---\n", ""),
        new("Link", "Insert a link", "[", "](url)"),
    ];

    [Inject]
    private INoteAccessor NoteAccessor { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private ICourseAccessor CourseAccessor { get; set; } = default!;

    [Inject]
    private ISemesterAccessor SemesterAccessor { get; set; } = default!;

    [Inject]
    private IPageHeaderStateHolder PageHeader { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "note")]
    [Parameter]
    public Guid? NoteIdQuery { get; set; }

    private IReadOnlyList<NoteDto>? NoteList { get; set; }

    private IReadOnlyList<CourseDto> Courses { get; set; } = [];

    private IReadOnlyList<SemesterDto> Semesters { get; set; } = [];

    private string SelectedScope { get; set; } = "all";

    private bool ShowArchived { get; set; }

    private string SearchText { get; set; } = string.Empty;

    private string? ActiveTag { get; set; }

    private Guid? SelectedNoteId { get; set; }

    private bool IsCreating { get; set; }

    private string WorkingTitle { get; set; } = string.Empty;

    private string WorkingContent { get; set; } = string.Empty;

    private string WorkingTags { get; set; } = string.Empty;

    private AssignmentTarget WorkingAssignmentKind { get; set; } = AssignmentTarget.Course;

    private Guid WorkingCourseId { get; set; }

    private Guid WorkingSemesterId { get; set; }

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private IReadOnlyList<NoteBacklinkDto> Backlinks { get; set; } = [];

    private bool ShowPreview { get; set; }

    private bool IsEditingContent { get; set; }

    private bool ShowEditor => IsCreating || IsEditingContent;

    private ElementReference ContentTextAreaRef;

    private ElementReference PreviewPaneRef;

    private bool SlashMenuOpen { get; set; }

    private int SlashTriggerPosition { get; set; }

    private string SlashQuery { get; set; } = string.Empty;

    private int SlashSelectedIndex { get; set; }

    private double SlashMenuTop { get; set; }

    private double SlashMenuLeft { get; set; }

    private int? PendingCursorPosition { get; set; }

    private IReadOnlyList<SlashCommand> FilteredSlashCommands =>
        SlashCommands.Where(c => c.Label.Contains(SlashQuery, StringComparison.OrdinalIgnoreCase)).ToList();

    private NoteDto? SelectedNote => NoteList?.FirstOrDefault(n => n.Id == SelectedNoteId);

    private bool IsReadOnly => !IsCreating && SelectedNote is { IsArchived: true };

    private IReadOnlyList<NoteDto>? _titleToNoteIdCacheSource;

    private Dictionary<string, Guid>? _titleToNoteIdCache;

    // NoteList only changes on load/save, but this property is read on every render (including
    // once per wiki-link match while rendering PreviewHtml), so rebuilding the dictionary from
    // scratch each time makes every keystroke in the editor cost O(notes) work for no reason.
    private Dictionary<string, Guid> TitleToNoteId
    {
        get
        {
            if (_titleToNoteIdCache is null || !ReferenceEquals(_titleToNoteIdCacheSource, NoteList))
            {
                _titleToNoteIdCacheSource = NoteList;
                _titleToNoteIdCache = (NoteList ?? []).ToDictionary(n => n.Title, n => n.Id, StringComparer.OrdinalIgnoreCase);
            }

            return _titleToNoteIdCache;
        }
    }

    private IReadOnlyList<string> AllTags =>
        (NoteList ?? [])
            .SelectMany(n => n.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private IReadOnlyList<NoteDto> VisibleNotes
    {
        get
        {
            if (NoteList is null)
            {
                return [];
            }

            IEnumerable<NoteDto> query = NoteList;

            if (!ShowArchived)
            {
                query = query.Where(n => !n.IsArchived);
            }

            query = SelectedScope switch
            {
                "all" => query,
                var s when s.StartsWith("course:", StringComparison.Ordinal)
                    => query.Where(n => n.CourseId == Guid.Parse(s["course:".Length..])),
                var s when s.StartsWith("semester:", StringComparison.Ordinal)
                    => query.Where(n => n.SemesterId == Guid.Parse(s["semester:".Length..])),
                _ => query,
            };

            if (ActiveTag is { } tag)
            {
                query = query.Where(n => n.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(n =>
                    n.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    n.Content.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    n.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            return query.OrderByDescending(n => n.UpdatedAt).ToList();
        }
    }

    private string? _previewHtmlCache;

    private string? _previewHtmlCacheContent;

    private Dictionary<string, Guid>? _previewHtmlCacheTitleMap;

    // Both HeadingOutline and the Razor markup read PreviewHtml, and each read previously reran
    // the wiki-link regex plus a full Markdig pass. Cache the result until WorkingContent (or the
    // resolved title map) actually changes, and resolve the title map once per call instead of
    // once per wiki-link match inside the Regex.Replace delegate.
    private string PreviewHtml
    {
        get
        {
            var titleMap = TitleToNoteId;

            if (_previewHtmlCache is not null
                && _previewHtmlCacheContent == WorkingContent
                && ReferenceEquals(_previewHtmlCacheTitleMap, titleMap))
            {
                return _previewHtmlCache;
            }

            var withLinks = WikiLinkParser.Pattern().Replace(WorkingContent, match =>
            {
                var title = match.Groups[1].Value.Trim();
                return titleMap.TryGetValue(title, out var id)
                    ? $"[{title}](/notes?note={id})"
                    : match.Value;
            });

            _previewHtmlCache = Markdown.ToHtml(withLinks, Pipeline);
            _previewHtmlCacheContent = WorkingContent;
            _previewHtmlCacheTitleMap = titleMap;

            return _previewHtmlCache;
        }
    }

    private IReadOnlyList<(int Level, string Text, string Id)> HeadingOutline
    {
        get
        {
            var outline = new List<(int Level, string Text, string Id)>();

            foreach (Match match in HeadingPattern.Matches(PreviewHtml))
            {
                var level = int.Parse(match.Groups[1].Value);
                var id = match.Groups[2].Value;
                var text = TagStripPattern.Replace(match.Groups[3].Value, string.Empty);
                outline.Add((level, text, id));
            }

            return outline;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        Courses = await CourseAccessor.GetAllAsync();
        Semesters = await SemesterAccessor.GetAllAsync();
        await LoadNotesAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (NoteIdQuery is { } id && id != SelectedNoteId)
        {
            await OpenNoteAsync(id);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!IsCreating && SelectedNote is null)
        {
            return;
        }

        if (ShowEditor)
        {
            await JS.InvokeVoidAsync("studyHubNotesEditor.attachSlashKeyGuard", ContentTextAreaRef);

            if (PendingCursorPosition is { } position)
            {
                PendingCursorPosition = null;
                await JS.InvokeVoidAsync("studyHubNotesEditor.setCursor", ContentTextAreaRef, position);
            }
        }

        if (!ShowEditor || ShowPreview)
        {
            await JS.InvokeVoidAsync("studyHubNotesEditor.highlightCode", PreviewPaneRef);
        }
    }

    private async Task OnContentInputAsync(ChangeEventArgs e)
    {
        WorkingContent = e.Value?.ToString() ?? string.Empty;
        var cursor = await JS.InvokeAsync<int>("studyHubNotesEditor.getSelectionStart", ContentTextAreaRef);
        await UpdateSlashMenuAsync(cursor);
    }

    private Task OnContentKeyDownAsync(KeyboardEventArgs e)
    {
        if (!SlashMenuOpen)
        {
            return Task.CompletedTask;
        }

        var commands = FilteredSlashCommands;

        return e.Key switch
        {
            "ArrowDown" when commands.Count > 0 => SetSlashSelection((SlashSelectedIndex + 1) % commands.Count),
            "ArrowUp" when commands.Count > 0 => SetSlashSelection((SlashSelectedIndex - 1 + commands.Count) % commands.Count),
            "Enter" or "Tab" when commands.Count > 0 => ApplySlashCommandAsync(commands[SlashSelectedIndex]),
            "Escape" => CloseSlashMenu(),
            _ => Task.CompletedTask,
        };
    }

    private Task SetSlashSelection(int index)
    {
        SlashSelectedIndex = index;
        return Task.CompletedTask;
    }

    private Task CloseSlashMenu()
    {
        SlashMenuOpen = false;
        return Task.CompletedTask;
    }

    private async Task UpdateSlashMenuAsync(int cursor)
    {
        cursor = Math.Clamp(cursor, 0, WorkingContent.Length);

        var lineStart = 0;
        if (cursor > 0)
        {
            lineStart = WorkingContent.LastIndexOf('\n', cursor - 1) + 1;
        }

        var textBeforeCursor = WorkingContent[lineStart..cursor];
        var slashIndex = textBeforeCursor.LastIndexOf('/');

        if (slashIndex < 0)
        {
            SlashMenuOpen = false;
            return;
        }

        var query = textBeforeCursor[(slashIndex + 1)..];
        if (query.Length > 24 || query.Any(char.IsWhiteSpace))
        {
            SlashMenuOpen = false;
            return;
        }

        SlashTriggerPosition = lineStart + slashIndex;
        SlashQuery = query;

        if (FilteredSlashCommands.Count == 0)
        {
            SlashMenuOpen = false;
            return;
        }

        if (!SlashMenuOpen)
        {
            SlashSelectedIndex = 0;
        }
        else
        {
            SlashSelectedIndex = Math.Min(SlashSelectedIndex, FilteredSlashCommands.Count - 1);
        }

        SlashMenuOpen = true;

        var coordinates = await JS.InvokeAsync<CaretCoordinates>("studyHubNotesEditor.getCaretCoordinates", ContentTextAreaRef);
        SlashMenuTop = coordinates.Top + coordinates.LineHeight;
        SlashMenuLeft = coordinates.Left;
    }

    private Task ApplySlashCommandAsync(SlashCommand command)
    {
        var before = WorkingContent[..SlashTriggerPosition];
        var queryEnd = Math.Min(SlashTriggerPosition + 1 + SlashQuery.Length, WorkingContent.Length);
        var after = WorkingContent[queryEnd..];

        WorkingContent = before + command.Before + command.After + after;
        PendingCursorPosition = before.Length + command.Before.Length;

        SlashMenuOpen = false;
        SlashQuery = string.Empty;
        return Task.CompletedTask;
    }

    private sealed record CaretCoordinates(double Top, double Left, double LineHeight);

    private async Task LoadNotesAsync()
    {
        NoteList = await NoteAccessor.GetAllAsync();
        PageHeader.SetHeader("Notes", $"{NoteList.Count} notes");
    }

    private void SetActiveTag(string? tag) => ActiveTag = tag;

    private void TogglePreview() => ShowPreview = !ShowPreview;

    private void StartEditing() => IsEditingContent = true;

    private void StartNewNote()
    {
        IsCreating = true;
        IsEditingContent = false;
        SelectedNoteId = null;
        WorkingTitle = string.Empty;
        WorkingContent = string.Empty;
        WorkingTags = string.Empty;
        WorkingAssignmentKind = AssignmentTarget.Course;
        WorkingCourseId = Guid.Empty;
        WorkingSemesterId = Guid.Empty;
        Backlinks = [];
        ErrorMessage = null;
    }

    private void ApplyTemplate(string key)
    {
        if (Templates.TryGetValue(key, out var content))
        {
            WorkingContent = content;
        }
    }

    private async Task OpenNoteAsync(Guid id)
    {
        SelectedNoteId = id;
        IsCreating = false;
        IsEditingContent = false;
        ErrorMessage = null;

        var note = NoteList?.FirstOrDefault(n => n.Id == id) ?? await NoteAccessor.GetByIdAsync(id);

        WorkingTitle = note.Title;
        WorkingContent = note.Content;
        WorkingTags = string.Join(", ", note.Tags);

        if (note.CourseId is { } courseId)
        {
            WorkingAssignmentKind = AssignmentTarget.Course;
            WorkingCourseId = courseId;
            WorkingSemesterId = Guid.Empty;
        }
        else
        {
            WorkingAssignmentKind = AssignmentTarget.Semester;
            WorkingSemesterId = note.SemesterId ?? Guid.Empty;
            WorkingCourseId = Guid.Empty;
        }

        Backlinks = await NoteAccessor.GetBacklinksAsync(id);
    }

    private async Task CancelEditingAsync()
    {
        if (IsCreating)
        {
            IsCreating = false;
            SelectedNoteId = null;
            ErrorMessage = null;
            return;
        }

        if (SelectedNoteId is { } id)
        {
            await OpenNoteAsync(id);
        }
    }

    private async Task SaveAsync()
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var courseId = WorkingAssignmentKind == AssignmentTarget.Course && WorkingCourseId != Guid.Empty ? WorkingCourseId : (Guid?)null;
            var semesterId = WorkingAssignmentKind == AssignmentTarget.Semester && WorkingSemesterId != Guid.Empty ? WorkingSemesterId : (Guid?)null;

            var saved = IsCreating
                ? await NoteAccessor.CreateAsync(new CreateNoteRequest(WorkingTitle, WorkingContent, WorkingTags, courseId, semesterId))
                : await NoteAccessor.UpdateAsync(new UpdateNoteRequest(SelectedNoteId!.Value, WorkingTitle, WorkingContent, WorkingTags, courseId, semesterId));

            await LoadNotesAsync();
            IsCreating = false;
            await OpenNoteAsync(saved.Id);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
            SelectedNoteId = null;
            IsCreating = false;
            await LoadNotesAsync();
        }
        catch (DuplicateNoteTitleException)
        {
            ErrorMessage = "A note with this title already exists.";
        }
        catch (NoteValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (NoteArchivedException)
        {
            ErrorMessage = "This note is archived. Restore it before editing.";
        }
        catch (CourseNotFoundException)
        {
            ErrorMessage = "Select a course.";
        }
        catch (CourseArchivedException)
        {
            ErrorMessage = "The selected course is archived. Choose an active course.";
        }
        catch (SemesterNotFoundException)
        {
            ErrorMessage = "Select a semester.";
        }
        catch (SemesterArchivedException)
        {
            ErrorMessage = "The selected semester is archived. Choose an active semester.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ArchiveSelectedAsync()
    {
        if (SelectedNoteId is not { } id)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await NoteAccessor.ArchiveAsync(id);
            await LoadNotesAsync();
            await OpenNoteAsync(id);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
            SelectedNoteId = null;
            await LoadNotesAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task RestoreSelectedAsync()
    {
        if (SelectedNoteId is not { } id)
        {
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            await NoteAccessor.RestoreAsync(id);
            await LoadNotesAsync();
            await OpenNoteAsync(id);
        }
        catch (NoteNotFoundException)
        {
            ErrorMessage = "This note no longer exists. It may have been deleted in another tab.";
            SelectedNoteId = null;
            await LoadNotesAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HandleNoteChangedAsync(NoteDto _) => await LoadNotesAsync();

    private static string GetExcerpt(NoteDto note)
    {
        var flattened = note.Content.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return flattened.Length > 80 ? string.Concat(flattened.AsSpan(0, 80), "…") : flattened;
    }

    private string GetParentName(NoteDto note) =>
        note.CourseId is { } courseId
            ? Courses.FirstOrDefault(c => c.Id == courseId)?.Name ?? "Unknown course"
            : Semesters.FirstOrDefault(s => s.Id == note.SemesterId)?.Name ?? "Unknown semester";
}
