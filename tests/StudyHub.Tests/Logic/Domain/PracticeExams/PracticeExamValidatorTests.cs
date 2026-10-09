using StudyHub.Logic.Domain;
using StudyHub.Shared.PracticeExams;
using StudyHub.Tests.Builders;

namespace StudyHub.Tests.Logic.Domain.PracticeExams;

public class PracticeExamValidatorTests
{
    private static readonly int[] SourceIds = [1, 2];

    private readonly PracticeExamValidator _sut = new();

    private IReadOnlyList<GeneratedPracticeExamTask> Filter(
        params GeneratedPracticeExamTask[] tasks
    ) => _sut.FilterGeneratedTasks(tasks, SourceIds);

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void ValidateGenerationOptions_WithAllowedDuration_Passes(int duration)
    {
        _sut.ValidateGenerationOptions(PracticeExamLevel.Gymnasium, duration, "nur Sortieren");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(180)]
    public void ValidateGenerationOptions_WithOtherDuration_Throws(int duration)
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.ValidateGenerationOptions(PracticeExamLevel.University, duration, null)
        );
    }

    [Fact]
    public void ValidateGenerationOptions_WithUnknownLevel_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.ValidateGenerationOptions((PracticeExamLevel)7, 60, null)
        );
    }

    [Fact]
    public void ValidateGenerationOptions_WithTooLongFocusHint_Throws()
    {
        Assert.Throws<PracticeExamValidationException>(() =>
            _sut.ValidateGenerationOptions(
                PracticeExamLevel.University,
                60,
                new string('x', GeneratePracticeExamRequest.FocusHintMaxLength + 1)
            )
        );
    }

    [Fact]
    public void FilterGeneratedTasks_KeepsValidTasksAndTrimsTheirText()
    {
        var singleChoice = PracticeExamBuilder.SingleChoice() with { Text = "  Frage  " };

        var tasks = Filter(singleChoice, PracticeExamBuilder.Open());

        Assert.Equal(2, tasks.Count);
        Assert.Equal("Frage", tasks[0].Text);
        Assert.Equal(4, tasks[0].Options.Count);
        Assert.Equal(PracticeExamTaskKind.Open, tasks[1].Kind);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void FilterGeneratedTasks_DropsSingleChoiceWithoutExactlyFourOptions(int optionCount)
    {
        Assert.Empty(Filter(PracticeExamBuilder.SingleChoice(optionCount: optionCount)));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsSingleChoiceWithoutACorrectOption()
    {
        Assert.Empty(Filter(PracticeExamBuilder.SingleChoice(correctIndex: -1)));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsSingleChoiceWithTwoCorrectOptions()
    {
        var task = PracticeExamBuilder.SingleChoice();
        var options = task.Options.Select((o, i) => o with { IsCorrect = i < 2 }).ToList();

        Assert.Empty(Filter(task with { Options = options }));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsSingleChoiceWithDuplicateOptions()
    {
        var task = PracticeExamBuilder.SingleChoice();
        var options = task.Options.ToList();
        options[3] = options[3] with { Text = " option 1 " };

        Assert.Empty(Filter(task with { Options = options }));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsSingleChoiceWithoutRationale()
    {
        var task = PracticeExamBuilder.SingleChoice();
        var options = task.Options.ToList();
        options[1] = options[1] with { Rationale = " " };

        Assert.Empty(Filter(task with { Options = options }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void FilterGeneratedTasks_DropsSingleChoiceWithPointsOutsideOneToThree(int points)
    {
        Assert.Empty(Filter(PracticeExamBuilder.SingleChoice(points: points)));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsOpenTaskWithoutCriteria()
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open() with { Criteria = [] }));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsOpenTaskWithMoreThanSixCriteria()
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open([1, 1, 1, 1, 1, 1, 1])));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsOpenTaskWithACriterionWorthNoPoints()
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open([2, 0])));
    }

    [Fact]
    public void FilterGeneratedTasks_DerivesOpenTaskPointsFromItsCriteria()
    {
        var task = PracticeExamBuilder.Open([2, 3]) with { Points = 10 };

        Assert.Equal(5, Assert.Single(Filter(task)).Points);
    }

    [Fact]
    public void FilterGeneratedTasks_DropsTaskWithMoreThanThirtyPoints()
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open([10, 10, 11])));
        Assert.Single(Filter(PracticeExamBuilder.Open([10, 10, 10])));
    }

    [Theory]
    [InlineData("", "Lösung")]
    [InlineData("Aufgabe", "  ")]
    public void FilterGeneratedTasks_DropsTaskWithEmptyTextOrSolution(string text, string solution)
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open() with { Text = text, Solution = solution }));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsTaskWithTooLongText()
    {
        var text = new string('x', PracticeExamTask.TextMaxLength + 1);

        Assert.Empty(Filter(PracticeExamBuilder.Open() with { Text = text }));
    }

    [Fact]
    public void FilterGeneratedTasks_DropsTaskOfUnknownKind()
    {
        Assert.Empty(Filter(PracticeExamBuilder.Open() with { Kind = null }));
    }

    [Fact]
    public void FilterGeneratedTasks_KeepsTaskWithUnknownSourceButDropsTheReference()
    {
        var task = Assert.Single(Filter(PracticeExamBuilder.Open(sourceId: 9)));

        Assert.Null(task.SourceId);
    }

    [Fact]
    public void FilterGeneratedTasks_KeepsAtMostThirtyTasks()
    {
        var tasks = Enumerable
            .Range(1, 35)
            .Select(i => PracticeExamBuilder.Open(text: $"Aufgabe {i}"))
            .ToArray();

        var filtered = Filter(tasks);

        Assert.Equal(PracticeExam.MaxTaskCount, filtered.Count);
        Assert.Equal("Aufgabe 30", filtered[^1].Text);
    }
}
