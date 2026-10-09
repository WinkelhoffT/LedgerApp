using System.Globalization;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain;

public sealed class PracticeExamLifecycle(
    TimeProvider timeProvider,
    ICalendarPeriodProvider calendarPeriodProvider,
    Random random
) : IPracticeExamLifecycle
{
    private const string TitlePrefix = "Probeklausur ";
    private const string Separator = " · ";

    public StoredPracticeExam Create(
        GeneratePracticeExamRequest request,
        string model,
        string promptVersion,
        PracticeExamMaterial material,
        IReadOnlyList<GeneratedPracticeExamTask> tasks
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var exam = new PracticeExam(
            Id: Guid.CreateVersion7(),
            Title: CreateTitle(material.SourceName, request.Level),
            Level: request.Level,
            DurationMinutes: request.DurationMinutes,
            SourceKind: request.SourceKind,
            CourseId: request.SourceKind == PracticeExamSourceKind.Course ? request.CourseId : null,
            DeckId: request.SourceKind == PracticeExamSourceKind.Deck ? request.DeckId : null,
            Model: model,
            PromptVersion: promptVersion,
            FocusHint: string.IsNullOrWhiteSpace(request.FocusHint)
                ? null
                : request.FocusHint.Trim(),
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now
        );

        var sourcesById = material.Sources.ToDictionary(source => source.Id);
        var storedTasks = tasks
            .Select(
                (task, index) =>
                    CreateTask(
                        exam.Id,
                        index + 1,
                        task,
                        task.SourceId is { } sourceId
                            ? sourcesById.GetValueOrDefault(sourceId)
                            : null,
                        now
                    )
            )
            .ToList();

        return new StoredPracticeExam(exam, storedTasks);
    }

    public PracticeExam Archive(PracticeExam exam) =>
        exam.IsArchived
            ? exam
            : exam with
            {
                IsArchived = true,
                UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };

    public PracticeExam Restore(PracticeExam exam) =>
        !exam.IsArchived
            ? exam
            : exam with
            {
                IsArchived = false,
                UpdatedAt = timeProvider.GetUtcNow().UtcDateTime,
            };

    public PracticeExamTask ExcludeTask(PracticeExam exam, PracticeExamTask task)
    {
        if (exam.IsArchived)
        {
            throw new PracticeExamArchivedException(exam.Id);
        }

        return task with
        {
            IsExcluded = true,
        };
    }

    private StoredPracticeExamTask CreateTask(
        Guid examId,
        int position,
        GeneratedPracticeExamTask task,
        PracticeExamSource? source,
        DateTime now
    )
    {
        var taskId = Guid.CreateVersion7();
        var criteria = task
            .Criteria.Select(
                (criterion, index) =>
                    new PracticeExamCriterion(
                        Guid.CreateVersion7(),
                        taskId,
                        index + 1,
                        criterion.Description,
                        criterion.Points
                    )
            )
            .ToList();

        return new StoredPracticeExamTask(
            new PracticeExamTask(
                Id: taskId,
                ExamId: examId,
                Position: position,
                Kind: task.Kind!.Value,
                Text: task.Text,
                Points: criteria.Count > 0 ? criteria.Sum(c => c.Points) : task.Points,
                Solution: task.Solution,
                SourceNoteId: source?.NoteId,
                SourceFlashcardId: source?.FlashcardId,
                IsExcluded: false,
                CreatedAt: now
            ),
            ShuffleOptions(taskId, task.Options),
            criteria
        );
    }

    // Language models tend to put the correct answer in the same position, so the order is
    // shuffled once when the exam is saved.
    private List<PracticeExamOption> ShuffleOptions(
        Guid taskId,
        IReadOnlyList<GeneratedPracticeExamOption> options
    )
    {
        var shuffled = options.ToArray();
        random.Shuffle(shuffled);

        return shuffled
            .Select(
                (option, index) =>
                    new PracticeExamOption(
                        Guid.CreateVersion7(),
                        taskId,
                        index + 1,
                        option.Text,
                        option.Rationale,
                        option.IsCorrect
                    )
            )
            .ToList();
    }

    private string CreateTitle(string sourceName, PracticeExamLevel level)
    {
        var suffix =
            Separator
            + GetLevelLabel(level)
            + Separator
            + calendarPeriodProvider
                .GetToday()
                .ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var maxNameLength = PracticeExam.TitleMaxLength - TitlePrefix.Length - suffix.Length;
        var name =
            sourceName.Length <= maxNameLength
                ? sourceName
                : sourceName[..(maxNameLength - 1)].TrimEnd() + "…";

        return TitlePrefix + name + suffix;
    }

    private static string GetLevelLabel(PracticeExamLevel level) =>
        level switch
        {
            PracticeExamLevel.SecondarySchool => "Oberschule",
            PracticeExamLevel.Gymnasium => "Gymnasium",
            _ => "Universität",
        };
}
