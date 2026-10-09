using StudyHub.Logic.Domain;
using StudyHub.Shared.Configuration;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.PracticeExams;

public class PracticeExamLifecycleTests
{
    // 23:30 UTC is already the next day in Berlin.
    private static readonly DateTime Now = new(2026, 10, 8, 23, 30, 0, DateTimeKind.Utc);
    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid NoteId = Guid.NewGuid();
    private static readonly Guid CardId = Guid.NewGuid();

    private readonly FixedTimeProvider _timeProvider = new(Now);

    private PracticeExamLifecycle CreateSut(int seed = 42) =>
        new(
            _timeProvider,
            new CalendarPeriodProvider(new CalendarOptions(), _timeProvider),
            new Random(seed)
        );

    private static GeneratePracticeExamRequest Request(
        PracticeExamSourceKind sourceKind = PracticeExamSourceKind.Course,
        string? focusHint = null
    ) =>
        new(
            sourceKind,
            CourseId,
            null,
            Guid.NewGuid(),
            PracticeExamLevel.University,
            60,
            focusHint
        );

    private static PracticeExamMaterial Material(string sourceName = "Algorithmen") =>
        new(
            sourceName,
            [
                new PracticeExamSource(1, "Dijkstra", "# Dijkstra", NoteId, null),
                new PracticeExamSource(2, string.Empty, "Front: F\nBack: B", null, CardId),
            ],
            100,
            0
        );

    [Fact]
    public void Create_StoresTheOptionsAndBuildsTheTitleFromSourceLevelAndLocalDate()
    {
        var created = CreateSut()
            .Create(
                Request(focusHint: "  nur Graphen "),
                "claude-opus-5-5",
                "practice-exam-v1",
                Material(),
                [PracticeExamBuilder.SingleChoice()]
            );

        var exam = created.Exam;
        Assert.Equal("Probeklausur Algorithmen · Universität · 09.10.2026", exam.Title);
        Assert.Equal(PracticeExamLevel.University, exam.Level);
        Assert.Equal(60, exam.DurationMinutes);
        Assert.Equal(CourseId, exam.CourseId);
        Assert.Null(exam.DeckId);
        Assert.Equal("claude-opus-5-5", exam.Model);
        Assert.Equal("practice-exam-v1", exam.PromptVersion);
        Assert.Equal("nur Graphen", exam.FocusHint);
        Assert.False(exam.IsArchived);
        Assert.Equal(Now, exam.CreatedAt);
    }

    [Fact]
    public void Create_ForADeck_SetsOnlyTheDeck()
    {
        var request = Request(PracticeExamSourceKind.Deck);

        var exam = CreateSut()
            .Create(request, "m", "v", Material(), [PracticeExamBuilder.Open()])
            .Exam;

        Assert.Null(exam.CourseId);
        Assert.Equal(request.DeckId, exam.DeckId);
    }

    [Fact]
    public void Create_ShortensALongSourceNameToFitTheTitle()
    {
        var exam = CreateSut()
            .Create(
                Request(),
                "m",
                "v",
                Material(new string('x', 300)),
                [PracticeExamBuilder.Open()]
            )
            .Exam;

        Assert.Equal(PracticeExam.TitleMaxLength, exam.Title.Length);
        Assert.Contains("x… · Universität", exam.Title);
    }

    [Fact]
    public void Create_NumbersTasksAndMapsSourcesBackToNotesAndCards()
    {
        var tasks = CreateSut()
            .Create(
                Request(),
                "m",
                "v",
                Material(),
                [
                    PracticeExamBuilder.SingleChoice(sourceId: 2),
                    PracticeExamBuilder.Open([2, 3]),
                    PracticeExamBuilder.Open(sourceId: null),
                ]
            )
            .Tasks;

        Assert.Equal([1, 2, 3], tasks.Select(t => t.Task.Position));
        Assert.Equal(CardId, tasks[0].Task.SourceFlashcardId);
        Assert.Null(tasks[0].Task.SourceNoteId);
        Assert.Equal(NoteId, tasks[1].Task.SourceNoteId);
        Assert.Null(tasks[2].Task.SourceNoteId);
        Assert.Null(tasks[2].Task.SourceFlashcardId);
    }

    [Fact]
    public void Create_TakesOpenTaskPointsFromTheCriteria()
    {
        var task = Assert.Single(
            CreateSut()
                .Create(Request(), "m", "v", Material(), [PracticeExamBuilder.Open([2, 3])])
                .Tasks
        );

        Assert.Equal(5, task.Task.Points);
        Assert.Equal([1, 2], task.Criteria.Select(c => c.Position));
        Assert.All(task.Criteria, c => Assert.Equal(task.Task.Id, c.TaskId));
        Assert.Empty(task.Options);
    }

    [Fact]
    public void Create_ShufflesTheOptionsWithoutChangingThem()
    {
        var generated = Enumerable
            .Range(0, 20)
            .Select(_ => PracticeExamBuilder.SingleChoice())
            .ToArray();

        var tasks = CreateSut().Create(Request(), "m", "v", Material(), generated).Tasks;

        Assert.Contains(tasks, t => !t.Options[0].IsCorrect);
        Assert.All(
            tasks,
            t =>
            {
                Assert.Equal([1, 2, 3, 4], t.Options.Select(o => o.Position));
                Assert.Equal(
                    ["Option 1", "Option 2", "Option 3", "Option 4"],
                    t.Options.Select(o => o.Text).Order()
                );
                Assert.Single(t.Options, o => o.IsCorrect);
                Assert.Equal("Option 1", t.Options.Single(o => o.IsCorrect).Text);
                Assert.Equal("Begründung 1", t.Options.Single(o => o.Text == "Option 1").Rationale);
            }
        );
    }

    [Fact]
    public void Create_WithTheSameSeed_ShufflesTheSameWay()
    {
        var generated = Enumerable
            .Range(0, 5)
            .Select(_ => PracticeExamBuilder.SingleChoice())
            .ToArray();

        var first = CreateSut(7).Create(Request(), "m", "v", Material(), generated).Tasks;
        var second = CreateSut(7).Create(Request(), "m", "v", Material(), generated).Tasks;

        Assert.Equal(
            first.Select(t => string.Join(",", t.Options.Select(o => o.Text))),
            second.Select(t => string.Join(",", t.Options.Select(o => o.Text)))
        );
    }

    [Fact]
    public void ArchiveAndRestore_ToggleTheFlag()
    {
        var sut = CreateSut();
        var exam = PracticeExamBuilder.Exam().Exam;

        var archived = sut.Archive(exam);
        var restored = sut.Restore(archived);

        Assert.True(archived.IsArchived);
        Assert.Equal(Now, archived.UpdatedAt);
        Assert.False(restored.IsArchived);
        Assert.Same(restored, sut.Restore(restored));
    }

    [Fact]
    public void ExcludeTask_MarksTheTask()
    {
        var exam = PracticeExamBuilder.Exam();

        var task = CreateSut().ExcludeTask(exam.Exam, exam.Tasks[0].Task);

        Assert.True(task.IsExcluded);
    }

    [Fact]
    public void ExcludeTask_OnAnArchivedExam_Throws()
    {
        var exam = PracticeExamBuilder.Exam(isArchived: true);

        Assert.Throws<PracticeExamArchivedException>(() =>
            CreateSut().ExcludeTask(exam.Exam, exam.Tasks[0].Task)
        );
    }
}
