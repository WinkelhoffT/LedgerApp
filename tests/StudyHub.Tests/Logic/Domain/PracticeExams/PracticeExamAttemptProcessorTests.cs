using StudyHub.Logic.Domain;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Domain.PracticeExams;

public class PracticeExamAttemptProcessorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly PracticeExamAttemptProcessor _sut;
    private readonly StoredPracticeExam _exam = PracticeExamBuilder.Exam();

    public PracticeExamAttemptProcessorTests()
    {
        _sut = new PracticeExamAttemptProcessor(_timeProvider);
    }

    private StoredPracticeExamTask SingleChoiceTask => _exam.Tasks[0];

    private StoredPracticeExamTask OpenTask => _exam.Tasks[1];

    private StoredPracticeExamAttempt StartNew(bool withTimeLimit = false) =>
        _sut.Start(_exam, null, withTimeLimit).Attempt;

    private StoredPracticeExamAttempt WithAnswer(
        StoredPracticeExamAttempt attempt,
        Guid taskId,
        Guid? optionId = null,
        string? text = null
    )
    {
        var answer = _sut.SaveAnswer(_exam, attempt, taskId, optionId, text);
        return attempt with
        {
            Answers = attempt.Answers.Select(a => a.Id == answer.Id ? answer : a).ToList(),
        };
    }

    [Fact]
    public void Start_CreatesAnAnswerPerTaskThatIsNotExcluded()
    {
        var exam = PracticeExamBuilder.Exam(excludeLastTask: true);

        var outcome = _sut.Start(exam, null, withTimeLimit: false);

        Assert.True(outcome.IsNew);
        Assert.Equal(
            [exam.Tasks[0].Task.Id, exam.Tasks[1].Task.Id],
            outcome.Attempt.Answers.Select(a => a.TaskId)
        );
        Assert.Equal(7, outcome.Attempt.Attempt.MaxPoints);
        Assert.Equal(Now, outcome.Attempt.Attempt.StartedAt);
        Assert.Null(outcome.Attempt.Attempt.DueAt);
        Assert.All(outcome.Attempt.Answers, a => Assert.Null(a.AwardedPoints));
    }

    [Fact]
    public void Start_WithTimeLimit_SetsTheDueTimeFromTheDuration()
    {
        Assert.Equal(Now.AddMinutes(60), StartNew(withTimeLimit: true).Attempt.DueAt);
    }

    [Fact]
    public void Start_WithAnOpenAttempt_ResumesIt()
    {
        var open = StartNew();

        var outcome = _sut.Start(PracticeExamBuilder.Exam(isArchived: true), open, true);

        Assert.False(outcome.IsNew);
        Assert.Same(open, outcome.Attempt);
    }

    [Fact]
    public void Start_OnAnArchivedExam_Throws()
    {
        Assert.Throws<PracticeExamArchivedException>(() =>
            _sut.Start(PracticeExamBuilder.Exam(isArchived: true), null, false)
        );
    }

    [Fact]
    public void Start_WhenEveryTaskIsExcluded_Throws()
    {
        var exam = _exam with
        {
            Tasks = _exam
                .Tasks.Select(t => t with { Task = t.Task with { IsExcluded = true } })
                .ToList(),
        };

        Assert.Throws<PracticeExamValidationException>(() => _sut.Start(exam, null, false));
    }

    [Fact]
    public void SaveAnswer_StoresTheOptionOrTheTextAsTyped()
    {
        var attempt = StartNew();
        var optionId = SingleChoiceTask.Options[0].Id;

        var choice = _sut.SaveAnswer(_exam, attempt, SingleChoiceTask.Task.Id, optionId, null);
        var text = _sut.SaveAnswer(
            _exam,
            attempt,
            OpenTask.Task.Id,
            null,
            "  code\n    eingerückt"
        );
        var cleared = _sut.SaveAnswer(_exam, attempt, OpenTask.Task.Id, null, "   ");

        Assert.Equal(optionId, choice.SelectedOptionId);
        Assert.Equal("  code\n    eingerückt", text.AnswerText);
        Assert.Null(cleared.AnswerText);
    }

    [Fact]
    public void SaveAnswer_WithinTheGracePeriodAfterTheTimeLimit_IsAccepted()
    {
        var attempt = StartNew(withTimeLimit: true);
        _timeProvider.UtcNow = Now.AddMinutes(61);

        var answer = _sut.SaveAnswer(_exam, attempt, OpenTask.Task.Id, null, "Antwort");

        Assert.Equal("Antwort", answer.AnswerText);
    }

    [Fact]
    public void SaveAnswer_AfterTheGracePeriod_ThrowsTimeOver()
    {
        var attempt = StartNew(withTimeLimit: true);
        _timeProvider.UtcNow = Now.AddMinutes(61).AddSeconds(1);

        Assert.Throws<PracticeExamTimeOverException>(() =>
            _sut.SaveAnswer(_exam, attempt, OpenTask.Task.Id, null, "Antwort")
        );
    }

    [Fact]
    public void SaveAnswer_AfterSubmission_Throws()
    {
        var attempt = _sut.Submit(_exam, StartNew());

        Assert.Throws<PracticeExamAttemptSubmittedException>(() =>
            _sut.SaveAnswer(_exam, attempt, OpenTask.Task.Id, null, "zu spät")
        );
    }

    [Fact]
    public void SaveAnswer_WithAnOptionOfAnotherTask_Throws()
    {
        var foreignOption = PracticeExamBuilder.SingleChoiceTask(Guid.NewGuid(), 1).Options[0].Id;

        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.SaveAnswer(_exam, StartNew(), SingleChoiceTask.Task.Id, foreignOption, null)
        );
    }

    [Fact]
    public void SaveAnswer_WithTextForASingleChoiceTask_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.SaveAnswer(_exam, StartNew(), SingleChoiceTask.Task.Id, null, "Option 2")
        );
    }

    [Fact]
    public void SaveAnswer_WithAnOptionForAnOpenTask_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.SaveAnswer(
                _exam,
                StartNew(),
                OpenTask.Task.Id,
                SingleChoiceTask.Options[0].Id,
                null
            )
        );
    }

    [Fact]
    public void SaveAnswer_WithTooLongText_Throws()
    {
        var text = new string('x', PracticeExamAnswer.AnswerTextMaxLength + 1);

        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.SaveAnswer(_exam, StartNew(), OpenTask.Task.Id, null, text)
        );
    }

    [Fact]
    public void SaveAnswer_ForATaskOutsideTheAttempt_Throws()
    {
        var exam = PracticeExamBuilder.Exam(excludeLastTask: true);
        var attempt = _sut.Start(exam, null, false).Attempt;

        Assert.Throws<PracticeExamTaskNotFoundException>(() =>
            _sut.SaveAnswer(exam, attempt, exam.Tasks[2].Task.Id, null, "Antwort")
        );
    }

    [Fact]
    public void Submit_GradesSingleChoiceAndGivesUnansweredOpenTasksZero()
    {
        var attempt = WithAnswer(
            StartNew(),
            SingleChoiceTask.Task.Id,
            SingleChoiceTask.Options[1].Id
        );
        attempt = WithAnswer(attempt, OpenTask.Task.Id, text: "Meine Antwort");
        _timeProvider.UtcNow = Now.AddMinutes(30);

        var submitted = _sut.Submit(_exam, attempt);

        Assert.Equal(Now.AddMinutes(30), submitted.Attempt.SubmittedAt);
        Assert.Equal([2, null, 0], submitted.Answers.Select(a => a.AwardedPoints));
        Assert.Null(submitted.Attempt.GradedAt);
        Assert.Null(submitted.Attempt.AwardedPoints);
    }

    [Fact]
    public void Submit_WithAWrongOrNoOption_GivesZeroAndCompletesWithoutOpenAnswers()
    {
        var attempt = WithAnswer(
            StartNew(),
            SingleChoiceTask.Task.Id,
            SingleChoiceTask.Options[0].Id
        );

        var submitted = _sut.Submit(_exam, attempt);

        Assert.Equal([0, 0, 0], submitted.Answers.Select(a => a.AwardedPoints));
        Assert.Equal(Now, submitted.Attempt.GradedAt);
        Assert.Equal(0, submitted.Attempt.AwardedPoints);
    }

    [Fact]
    public void Submit_Twice_ChangesNothing()
    {
        var submitted = _sut.Submit(_exam, StartNew());
        _timeProvider.UtcNow = Now.AddHours(1);

        Assert.Same(submitted, _sut.Submit(_exam, submitted));
    }

    [Fact]
    public void Grade_AwardsThePointsOfTheTickedCriteria()
    {
        var attempt = _sut.Submit(_exam, WithAnswer(StartNew(), OpenTask.Task.Id, text: "Antwort"));
        var criterion = OpenTask.Criteria[1];

        var graded = _sut.Grade(_exam, attempt, OpenTask.Task.Id, [criterion.Id, criterion.Id]);

        var answer = graded.Answers.Single(a => a.TaskId == OpenTask.Task.Id);
        Assert.Equal(3, answer.AwardedPoints);
        Assert.Equal(
            [new PracticeExamAnswerCriterion(answer.Id, criterion.Id)],
            graded.MetCriteria
        );
    }

    [Fact]
    public void Grade_CompletesTheAttemptWithTheLastOpenTaskAndAllowsChangesAfterwards()
    {
        var attempt = WithAnswer(
            StartNew(),
            SingleChoiceTask.Task.Id,
            SingleChoiceTask.Options[1].Id
        );
        attempt = WithAnswer(attempt, OpenTask.Task.Id, text: "Antwort");
        attempt = WithAnswer(attempt, _exam.Tasks[2].Task.Id, text: "Antwort");
        attempt = _sut.Submit(_exam, attempt);

        attempt = _sut.Grade(_exam, attempt, OpenTask.Task.Id, [OpenTask.Criteria[0].Id]);
        Assert.Null(attempt.Attempt.GradedAt);

        _timeProvider.UtcNow = Now.AddMinutes(5);
        attempt = _sut.Grade(_exam, attempt, _exam.Tasks[2].Task.Id, []);
        Assert.Equal(Now.AddMinutes(5), attempt.Attempt.GradedAt);
        Assert.Equal(2 + 2 + 0, attempt.Attempt.AwardedPoints);

        _timeProvider.UtcNow = Now.AddMinutes(10);
        attempt = _sut.Grade(
            _exam,
            attempt,
            OpenTask.Task.Id,
            [OpenTask.Criteria[0].Id, OpenTask.Criteria[1].Id]
        );
        Assert.Equal(Now.AddMinutes(5), attempt.Attempt.GradedAt);
        Assert.Equal(2 + 5 + 0, attempt.Attempt.AwardedPoints);
        Assert.Equal(2, attempt.MetCriteria.Count);
    }

    [Fact]
    public void Grade_WithACriterionOfAnotherTask_Throws()
    {
        var attempt = _sut.Submit(_exam, WithAnswer(StartNew(), OpenTask.Task.Id, text: "Antwort"));

        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.Grade(_exam, attempt, OpenTask.Task.Id, [_exam.Tasks[2].Criteria[0].Id])
        );
    }

    [Fact]
    public void Grade_BeforeSubmission_Throws()
    {
        var attempt = WithAnswer(StartNew(), OpenTask.Task.Id, text: "Antwort");

        Assert.Throws<PracticeExamAttemptNotSubmittedException>(() =>
            _sut.Grade(_exam, attempt, OpenTask.Task.Id, [])
        );
    }

    [Fact]
    public void Grade_ASingleChoiceOrUnansweredTask_Throws()
    {
        var attempt = _sut.Submit(_exam, StartNew());

        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.Grade(_exam, attempt, SingleChoiceTask.Task.Id, [])
        );
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.Grade(_exam, attempt, OpenTask.Task.Id, [])
        );
    }

    [Fact]
    public void GetStatus_FollowsTheTimestamps()
    {
        Assert.Equal(
            PracticeExamAttemptStatus.InProgress,
            _sut.GetStatus(PracticeExamBuilder.Attempt())
        );
        Assert.Equal(
            PracticeExamAttemptStatus.Submitted,
            _sut.GetStatus(PracticeExamBuilder.Attempt(submittedAt: Now))
        );
        Assert.Equal(
            PracticeExamAttemptStatus.Graded,
            _sut.GetStatus(PracticeExamBuilder.Attempt(submittedAt: Now, gradedAt: Now))
        );
    }

    [Theory]
    [InlineData(60, 41, 68)]
    [InlineData(8, 5, 63)]
    [InlineData(10, 0, 0)]
    public void GetPercent_RoundsTheShareOfTheMaximumPoints(int max, int awarded, int percent)
    {
        Assert.Equal(percent, _sut.GetPercent(PracticeExamBuilder.Attempt(max, awarded)));
    }

    [Fact]
    public void GetPercent_BeforeGrading_IsNull()
    {
        Assert.Null(_sut.GetPercent(PracticeExamBuilder.Attempt()));
    }

    [Fact]
    public void SelectBest_PicksTheHighestPercentageAndTheLatestOnATie()
    {
        var low = PracticeExamBuilder.Attempt(60, 30, Now);
        var high = PracticeExamBuilder.Attempt(10, 8, Now.AddDays(1));
        var sameLater = PracticeExamBuilder.Attempt(5, 4, Now.AddDays(2));
        var ungraded = PracticeExamBuilder.Attempt(10, null, Now.AddDays(3));

        Assert.Same(sameLater, _sut.SelectBest([low, high, sameLater, ungraded]));
        Assert.Null(_sut.SelectBest([ungraded]));
    }
}
