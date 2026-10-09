using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StudyHub.Api;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Logic.Integration.PracticeExams;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Api;
using StudyHub.Tests.Builders;

namespace StudyHub.Tests.Logic.Integration.PracticeExams;

/// <summary>Runs the accessors against the real StudyHub.Api, so both sides of the wire contract are covered.</summary>
public class PracticeExamAccessorTests
{
    private static WebApplicationFactory<Program> CreateFactory(bool withGenerator = true)
    {
        var generator = new Mock<IPracticeExamGenerator>();
        generator.SetupGet(g => g.PromptVersion).Returns("practice-exam-v1");
        generator
            .Setup(g =>
                g.GenerateAsync(
                    It.IsAny<PracticeExamGenerationInput>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([PracticeExamBuilder.SingleChoice(), PracticeExamBuilder.Open([2, 3])]);

        return InMemoryApiFactory
            .Create()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    if (withGenerator)
                    {
                        services.RemoveAll<IPracticeExamGenerator>();
                        services.AddSingleton(generator.Object);
                    }
                })
            );
    }

    private static async Task<Guid> CreateCourseWithNoteAsync(HttpClient client)
    {
        var semester = await (
            await client.PostAsJsonAsync(
                "api/semesters",
                new CreateSemesterRequest(
                    "Winter 2026/27",
                    new DateOnly(2026, 10, 1),
                    new DateOnly(2027, 3, 31)
                )
            )
        ).Content.ReadFromJsonAsync<SemesterDto>();
        var course = await (
            await client.PostAsJsonAsync(
                "api/courses",
                new CreateCourseRequest("Algorithmen", null, "#2563eb", semester!.Id)
            )
        ).Content.ReadFromJsonAsync<CourseDto>();
        (
            await client.PostAsJsonAsync(
                "api/notes",
                new CreateNoteRequest("Dijkstra", "# Dijkstra", null, course!.Id, null)
            )
        ).EnsureSuccessStatusCode();
        return course.Id;
    }

    private static GeneratePracticeExamRequest Request(Guid courseId) =>
        new(
            PracticeExamSourceKind.Course,
            courseId,
            null,
            null,
            PracticeExamLevel.SecondarySchool,
            30,
            null
        );

    [Fact]
    public async Task GenerateWriteAndGrade_RoundTripsThroughTheApi()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var generation = new PracticeExamGenerationAccessor(client);
        var exams = new PracticeExamAccessor(client);
        var attempts = new PracticeExamAttemptAccessor(client);
        var courseId = await CreateCourseWithNoteAsync(client);

        var models = await generation.GetModelsAsync();
        var exam = await generation.GenerateAsync(Request(courseId));
        var sheet = await attempts.StartAsync(exam.Id, new StartPracticeExamAttemptRequest(false));
        var open = sheet.Tasks.Single(t => t.Kind == PracticeExamTaskKind.Open);
        await attempts.SaveAnswerAsync(
            sheet.AttemptId,
            open.TaskId,
            new SavePracticeExamAnswerRequest(null, "Antwort")
        );
        var reloaded = await attempts.GetSheetAsync(sheet.AttemptId);
        var submitted = await attempts.SubmitAsync(sheet.AttemptId);
        var criterion = submitted.Tasks.Single(t => t.TaskId == open.TaskId).Criteria[0];
        var graded = await attempts.GradeAsync(
            sheet.AttemptId,
            open.TaskId,
            new GradePracticeExamAnswerRequest([criterion.Id])
        );
        var review = await attempts.GetReviewAsync(sheet.AttemptId);
        var cover = await exams.GetByIdAsync(exam.Id);
        var history = await exams.GetAttemptsAsync(exam.Id);

        Assert.Contains(models, m => m.IsDefault);
        Assert.Equal(PracticeExamLevel.SecondarySchool, exam.Level);
        Assert.Equal("Antwort", reloaded.Tasks.Single(t => t.TaskId == open.TaskId).AnswerText);
        Assert.Equal(PracticeExamAttemptStatus.Submitted, submitted.Status);
        Assert.Equal(PracticeExamAttemptStatus.Graded, graded.Status);
        Assert.Equal(2, review.AwardedPoints);
        Assert.True(review.Tasks.Single(t => t.TaskId == open.TaskId).Criteria[0].IsMet);
        Assert.Equal(2, cover.BestAttempt?.AwardedPoints);
        Assert.Equal(sheet.AttemptId, Assert.Single(history).Id);
    }

    [Fact]
    public async Task ArchiveRestoreAndExclude_UpdateTheExam()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var exams = new PracticeExamAccessor(client);
        var exam = await new PracticeExamGenerationAccessor(client).GenerateAsync(
            Request(await CreateCourseWithNoteAsync(client))
        );
        var sheet = await new PracticeExamAttemptAccessor(client).StartAsync(
            exam.Id,
            new StartPracticeExamAttemptRequest(false)
        );

        var excluded = await exams.ExcludeTaskAsync(sheet.Tasks[0].TaskId);
        var archived = await exams.ArchiveAsync(exam.Id);
        var activeOnly = await exams.GetAllAsync(includeArchived: false);
        var restored = await exams.RestoreAsync(exam.Id);

        Assert.Equal(1, excluded.ExcludedTaskCount);
        Assert.True(archived.IsArchived);
        Assert.Empty(activeOnly);
        Assert.False(restored.IsArchived);
        Assert.Single(await exams.GetAllAsync(includeArchived: false));
    }

    [Fact]
    public async Task Errors_AreMappedToTheSharedExceptions()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var generation = new PracticeExamGenerationAccessor(client);
        var attempts = new PracticeExamAttemptAccessor(client);
        var courseId = await CreateCourseWithNoteAsync(client);
        var exam = await generation.GenerateAsync(Request(courseId));
        var sheet = await attempts.StartAsync(exam.Id, new StartPracticeExamAttemptRequest(false));

        var notFound = await Assert.ThrowsAsync<PracticeExamNotFoundException>(() =>
            new PracticeExamAccessor(client).GetByIdAsync(Guid.NewGuid())
        );
        await Assert.ThrowsAsync<PracticeExamValidationException>(() =>
            generation.GenerateAsync(Request(courseId) with { DurationMinutes = 10 })
        );
        await Assert.ThrowsAsync<CourseNotFoundException>(() =>
            generation.GenerateAsync(Request(Guid.NewGuid()))
        );
        await Assert.ThrowsAsync<FlashcardDeckNotFoundException>(() =>
            generation.GenerateAsync(
                Request(courseId) with
                {
                    SourceKind = PracticeExamSourceKind.Deck,
                    DeckId = Guid.NewGuid(),
                }
            )
        );
        await Assert.ThrowsAsync<PracticeExamAttemptNotSubmittedException>(() =>
            attempts.GetReviewAsync(sheet.AttemptId)
        );
        await Assert.ThrowsAsync<PracticeExamTaskNotFoundException>(() =>
            attempts.SaveAnswerAsync(
                sheet.AttemptId,
                Guid.NewGuid(),
                new SavePracticeExamAnswerRequest(null, "x")
            )
        );
        await attempts.SubmitAsync(sheet.AttemptId);
        var submitted = await Assert.ThrowsAsync<PracticeExamAttemptSubmittedException>(() =>
            attempts.GetSheetAsync(sheet.AttemptId)
        );
        await Assert.ThrowsAsync<PracticeExamAttemptNotFoundException>(() =>
            attempts.SubmitAsync(Guid.NewGuid())
        );

        Assert.NotEqual(Guid.Empty, notFound.ExamId);
        Assert.Equal(sheet.AttemptId, submitted.AttemptId);
    }

    [Fact]
    public async Task Generate_WithoutApiKey_ThrowsAiNotConfigured()
    {
        using var factory = CreateFactory(withGenerator: false);
        var client = factory.CreateClient();

        await Assert.ThrowsAsync<AiNotConfiguredException>(async () =>
            await new PracticeExamGenerationAccessor(client).GenerateAsync(
                Request(await CreateCourseWithNoteAsync(client))
            )
        );
    }
}
