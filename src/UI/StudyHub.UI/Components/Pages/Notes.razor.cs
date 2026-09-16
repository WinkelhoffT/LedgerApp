using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Components;
using StudyHub.Logic.Integration.Courses;
using StudyHub.Logic.Integration.Notes;
using StudyHub.Logic.Integration.Semesters;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;
using StudyHub.UI.Services;

namespace StudyHub.UI.Components.Pages;

public partial class Notes
{
    private enum AssignmentTarget { Course, Semester }

    private static readonly Regex WikiLinkPattern = new(@"\[\[(.+?)\]\]", RegexOptions.Compiled);
    private static readonly Regex HeadingPattern = new(@"<h([1-3]) id=""([^""]+)"">(.*?)</h\1>", RegexOptions.Compiled);
    private static readonly Regex TagStripPattern = new("<.*?>", RegexOptions.Compiled);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().UseAutoIdentifiers().Build();

    private static readonly Dictionary<string, string> Templates = new()
    {
        ["blank"] = "",
        ["lecture"] = "# Lecture Notes\n\n## Topic\n\n## Key Points\n\n- \n\n## Open Questions\n\n- \n",
        ["exam"] = "# Exam Prep\n\n## Concepts to Review\n\n- \n\n## Practice Questions\n\n1. \n\n## Summary\n\n",
    };

    [Inject]
    private INoteAccessor NoteAccessor { get; set; } = default!;

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

    private NoteDto? SelectedNote => NoteList?.FirstOrDefault(n => n.Id == SelectedNoteId);

    private bool IsReadOnly => !IsCreating && SelectedNote is { IsArchived: true };

    private Dictionary<string, Guid> TitleToNoteId =>
        (NoteList ?? []).ToDictionary(n => n.Title, n => n.Id, StringComparer.OrdinalIgnoreCase);

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

    private string PreviewHtml
    {
        get
        {
            var withLinks = WikiLinkPattern.Replace(WorkingContent, match =>
            {
                var title = match.Groups[1].Value.Trim();
                return TitleToNoteId.TryGetValue(title, out var id)
                    ? $"[{title}](/notes?note={id})"
                    : match.Value;
            });

            return Markdown.ToHtml(withLinks, Pipeline);
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

    private async Task LoadNotesAsync()
    {
        NoteList = await NoteAccessor.GetAllAsync();
        PageHeader.SetHeader("Notes", $"{NoteList.Count} notes");
    }

    private void SetActiveTag(string? tag) => ActiveTag = tag;

    private void StartNewNote()
    {
        IsCreating = true;
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
        await NoteAccessor.ArchiveAsync(id);
        await LoadNotesAsync();
        await OpenNoteAsync(id);
        IsSaving = false;
    }

    private async Task RestoreSelectedAsync()
    {
        if (SelectedNoteId is not { } id)
        {
            return;
        }

        IsSaving = true;
        await NoteAccessor.RestoreAsync(id);
        await LoadNotesAsync();
        await OpenNoteAsync(id);
        IsSaving = false;
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
