using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Logic.Business.PracticeExams;

public class PracticeExamAttemptOrchestratorTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IPracticeExamRepository> _practiceExamRepository = new();
    private readonly Mock<IPracticeExamAttemptRepository> _attemptRepository = new();
    private readonly Mock<INoteRepository> _noteRepository = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepository = new();
    private readonly PracticeExamAttemptProcessor _processor = new(new FixedTimeProvider(Now));
    private readonly PracticeExamAttemptOrchestrator _sut;
    private readonly StoredPracticeExam _exam;
    private readonly Note _note;
    private readonly Flashcard _card;

    public PracticeExamAttemptOrchestratorTests()
    {
        _sut = new PracticeExamAttemptOrchestrator(
            _practiceExamRepository.Object,
            _attemptRepository.Object,
            _processor,
            _noteRepository.Object,
            _flashcardRepository.Object
        );

        var exam = PracticeExamBuilder.Exam();
        _note = new Note(
            Guid.NewGuid(),
            "Dijkstra",
            "# Dijkstra",
            null,
            exam.Exam.CourseId,
            null,
            false,
            Now,
            Now
        );
        _card = new Flashcard(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Was ist BFS?",
            "Breitensuche",
            null,
            null,
            FlashcardState.New,
            0,
            Now,
            0,
            2500,
            0,
            0,
            null,
            Now,
            Now
        );
        _exam = exam with
        {
            Tasks =
            [
                exam.Tasks[0] with
                {
                    Task = exam.Tasks[0].Task with { SourceNoteId = _note.Id },
                },
                exam.Tasks[1] with
                {
                    Task = exam.Tasks[1].Task with { SourceFlashcardId = _card.Id },
                },
                exam.Tasks[2],
            ],
        };

        _practiceExamRepository
            .Setup(r => r.GetWithTasksAsync(_exam.Exam.Id, default))
            .ReturnsAsync(_exam);
        _noteRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default))
            .ReturnsAsync([_note]);
        _flashcardRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default))
            .ReturnsAsync([_card]);
    }

    private StoredPracticeExamAttempt SetupAttempt(
        Func<StoredPracticeExamAttempt, StoredPracticeExamAttempt>? change = null
    )
    {
        var attempt = _processor.Start(_exam, null, withTimeLimit: false).Attempt;
        attempt = change?.Invoke(attempt) ?? attempt;
        _attemptRepository
            .Setup(r => r.GetWithAnswersAsync(attempt.Attempt.Id, default))
            .ReturnsAsync(attempt);
        return attempt;
    }

    private StoredPracticeExamAttempt Answer(
        StoredPracticeExamAttempt attempt,
        int taskIndex,
        Guid? optionId = null,
        string? text = null
    )
    {
        var answer = _processor.SaveAnswer(
            _exam,
            attempt,
            _exam.Tasks[taskIndex].Task.Id,
            optionId,
            text
        );
        return attempt with
        {
            Answers = attempt.Answers.Select(a => a.Id == answer.Id ? answer : a).ToList(),
        };
    }

    [Fact]
    public async Task StartAsync_StartsANewAttemptAndReturnsTheSheetWithoutSolutions()
    {
        StoredPracticeExamAttempt? added = null;
        _attemptRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredPracticeExamAttempt>(), default))
            .Callback<StoredPracticeExamAttempt, CancellationToken>((a, _) => added = a);

        var sheet = await _sut.StartAsync(_exam.Exam.Id, new StartPracticeExamAttemptRequest(true));

        Assert.NotNull(added);
        Assert.Equal(added.Attempt.Id, sheet.AttemptId);
        Assert.Equal(Now.AddMinutes(60), sheet.DueAt);
        Assert.Equal(2 + 5 + 4, sheet.MaxPoints);
        Assert.Equal([1, 2, 3], sheet.Tasks.Select(t => t.Number));
        Assert.Equal(
            _exam.Tasks[0].Options.Select(o => o.Text),
            sheet.Tasks[0].Options.Select(o => o.Text)
        );
        Assert.Empty(sheet.Tasks[1].Options);
        _attemptRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(typeof(PracticeExamSheetDto))]
    [InlineData(typeof(PracticeExamSheetTaskDto))]
    [InlineData(typeof(PracticeExamSheetOptionDto))]
    public void SheetDtos_CarryNoSolutionsOrCorrectness(Type type)
    {
        string[] forbidden = ["Solution", "IsCorrect", "Rationale", "Criteria", "AwardedPoints"];

        Assert.DoesNotContain(type.GetProperties(), p => forbidden.Contains(p.Name));
    }

    [Fact]
    public async Task StartAsync_WithAnOpenAttempt_ResumesItWithItsAnswers()
    {
        var open = Answer(SetupAttempt(), 1, text: "Meine Antwort");
        _attemptRepository
            .Setup(r => r.GetOpenAttemptAsync(_exam.Exam.Id, default))
            .ReturnsAsync(open);

        var sheet = await _sut.StartAsync(_exam.Exam.Id, new StartPracticeExamAttemptRequest(true));

        Assert.Equal(open.Attempt.Id, sheet.AttemptId);
        Assert.Null(sheet.DueAt);
        Assert.Equal("Meine Antwort", sheet.Tasks[1].AnswerText);
        _attemptRepository.Verify(
            r => r.AddAsync(It.IsAny<StoredPracticeExamAttempt>(), default),
            Times.Never
        );
    }

    [Fact]
    public async Task StartAsync_WithUnknownExam_Throws()
    {
        await Assert.ThrowsAsync<PracticeExamNotFoundException>(() =>
            _sut.StartAsync(Guid.NewGuid(), new StartPracticeExamAttemptRequest(false))
        );
    }

    [Fact]
    public async Task SaveAnswerAsync_UpdatesTheAnswer()
    {
        var attempt = SetupAttempt();
        var option = _exam.Tasks[0].Options[1];

        await _sut.SaveAnswerAsync(
            attempt.Attempt.Id,
            _exam.Tasks[0].Task.Id,
            new SavePracticeExamAnswerRequest(option.Id, null)
        );

        _attemptRepository.Verify(
            r => r.UpdateAnswer(It.Is<PracticeExamAnswer>(a => a.SelectedOptionId == option.Id)),
            Times.Once
        );
        _attemptRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SaveAnswerAsync_WithUnknownAttempt_Throws()
    {
        await Assert.ThrowsAsync<PracticeExamAttemptNotFoundException>(() =>
            _sut.SaveAnswerAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new SavePracticeExamAnswerRequest(null, "x")
            )
        );
    }

    [Fact]
    public async Task SubmitAsync_SavesThePointsAndReturnsTheReview()
    {
        var attempt = SetupAttempt(a =>
            Answer(Answer(a, 0, _exam.Tasks[0].Options[1].Id), 1, text: "Antwort")
        );

        var review = await _sut.SubmitAsync(attempt.Attempt.Id);

        Assert.Equal(PracticeExamAttemptStatus.Submitted, review.Status);
        Assert.Equal([2, null, 0], review.Tasks.Select(t => t.AwardedPoints));
        Assert.Equal(1, review.OpenTaskCount);
        Assert.Equal(0, review.GradedOpenTaskCount);
        Assert.Equal("Erklärung", review.Tasks[0].Solution);
        Assert.True(review.Tasks[0].Options[1].IsCorrect);
        Assert.Equal("Begründung 2", review.Tasks[0].Options[1].Rationale);
        Assert.Equal(
            new PracticeExamSourceReferenceDto(_note.Id, null, null, "Dijkstra"),
            review.Tasks[0].Source
        );
        Assert.Equal(
            new PracticeExamSourceReferenceDto(null, _card.Id, _card.DeckId, "Was ist BFS?"),
            review.Tasks[1].Source
        );
        Assert.Null(review.Tasks[2].Source);
        _attemptRepository.Verify(
            r => r.Update(It.Is<PracticeExamAttempt>(a => a.SubmittedAt == Now)),
            Times.Once
        );
        _attemptRepository.Verify(
            r => r.UpdateAnswer(It.IsAny<PracticeExamAnswer>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenAlreadySubmitted_SavesNothing()
    {
        var attempt = SetupAttempt(a => _processor.Submit(_exam, a));

        await _sut.SubmitAsync(attempt.Attempt.Id);

        _attemptRepository.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetSheetAsync_AfterSubmission_Throws()
    {
        var attempt = SetupAttempt(a => _processor.Submit(_exam, a));

        await Assert.ThrowsAsync<PracticeExamAttemptSubmittedException>(() =>
            _sut.GetSheetAsync(attempt.Attempt.Id)
        );
    }

    [Fact]
    public async Task GetReviewAsync_BeforeSubmission_Throws()
    {
        var attempt = SetupAttempt();

        await Assert.ThrowsAsync<PracticeExamAttemptNotSubmittedException>(() =>
            _sut.GetReviewAsync(attempt.Attempt.Id)
        );
    }

    [Fact]
    public async Task GradeAsync_StoresTheTickedCriteriaAndTheirPoints()
    {
        var attempt = SetupAttempt(a => _processor.Submit(_exam, Answer(a, 1, text: "Antwort")));
        var task = _exam.Tasks[1];

        var review = await _sut.GradeAsync(
            attempt.Attempt.Id,
            task.Task.Id,
            new GradePracticeExamAnswerRequest([task.Criteria[1].Id])
        );

        var answerId = attempt.Answers.Single(a => a.TaskId == task.Task.Id).Id;
        _attemptRepository.Verify(
            r =>
                r.ReplaceMetCriteriaAsync(
                    answerId,
                    It.Is<IReadOnlyCollection<Guid>>(ids =>
                        ids.SequenceEqual(new[] { task.Criteria[1].Id })
                    ),
                    default
                ),
            Times.Once
        );
        _attemptRepository.Verify(
            r =>
                r.UpdateAnswer(
                    It.Is<PracticeExamAnswer>(a => a.Id == answerId && a.AwardedPoints == 3)
                ),
            Times.Once
        );
        Assert.Equal(PracticeExamAttemptStatus.Graded, review.Status);
        Assert.Equal(3, review.AwardedPoints);
        Assert.Equal(27, review.Percent);
        Assert.Equal([false, true], review.Tasks[1].Criteria.Select(c => c.IsMet));
        Assert.Equal(1, review.GradedOpenTaskCount);
    }
}
