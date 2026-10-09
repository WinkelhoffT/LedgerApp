using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StudyHub.Api;
using StudyHub.Logic.Integration.Ai;
using StudyHub.Shared.Ai;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Notes;
using StudyHub.Shared.PracticeExams;
using StudyHub.Shared.Semesters;
using StudyHub.Tests.Builders;
using StudyHub.Tests.Fakes;

namespace StudyHub.Tests.Api.PracticeExams;

public class PracticeExamEndpointsTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private readonly FixedTimeProvider _timeProvider = new(Now);

    private WebApplicationFactory<Program> CreateFactory(
        IPracticeExamGenerator? generator = null
    ) =>
        InMemoryApiFactory
            .Create()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(_timeProvider);
                    if (generator is not null)
                    {
                        services.RemoveAll<IPracticeExamGenerator>();
                        services.AddSingleton(generator);
                    }
                })
            );

    private static IPracticeExamGenerator FakeGenerator(params GeneratedPracticeExamTask[] tasks)
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
            .ReturnsAsync(
                tasks.Length > 0
                    ? tasks
                    : [PracticeExamBuilder.SingleChoice(), PracticeExamBuilder.Open([2, 3])]
            );
        return generator.Object;
    }

    private static async Task<CourseDto> CreateCourseWithNoteAsync(HttpClient client)
    {
        var semesterResponse = await client.PostAsJsonAsync(
            "api/semesters",
            new CreateSemesterRequest(
                "Winter 2026/27",
                new DateOnly(2026, 10, 1),
                new DateOnly(2027, 3, 31)
            )
        );
        var semester = await semesterResponse.Content.ReadFromJsonAsync<SemesterDto>();

        var courseResponse = await client.PostAsJsonAsync(
            "api/courses",
            new CreateCourseRequest("Algorithmen", null, "#2563eb", semester!.Id)
        );
        var course = (await courseResponse.Content.ReadFromJsonAsync<CourseDto>())!;

        var noteResponse = await client.PostAsJsonAsync(
            "api/notes",
            new CreateNoteRequest(
                "Dijkstra",
                "# Dijkstra\nNur nicht-negative Kanten.",
                null,
                course.Id,
                null
            )
        );
        noteResponse.EnsureSuccessStatusCode();
        return course;
    }

    private static GeneratePracticeExamRequest Request(Guid courseId, int durationMinutes = 60) =>
        new(
            PracticeExamSourceKind.Course,
            courseId,
            null,
            null,
            PracticeExamLevel.University,
            durationMinutes,
            null
        );

    private static async Task<PracticeExamDto> GenerateAsync(HttpClient client)
    {
        var course = await CreateCourseWithNoteAsync(client);
        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(course.Id)
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PracticeExamDto>())!;
    }

    private static async Task<PracticeExamSheetDto> StartAsync(
        HttpClient client,
        Guid examId,
        bool withTimeLimit = false
    )
    {
        var response = await client.PostAsJsonAsync(
            $"api/practice-exams/{examId}/attempts",
            new StartPracticeExamAttemptRequest(withTimeLimit)
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PracticeExamSheetDto>())!;
    }

    private static async Task<string?> GetProblemValueAsync(
        HttpResponseMessage response,
        string key
    )
    {
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return
            problemDetails!.Extensions.TryGetValue(key, out var value)
            && value is JsonElement element
            ? element.GetString()
            : null;
    }

    [Fact]
    public async Task Generate_Returns201WithTheSavedExam()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var course = await CreateCourseWithNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(course.Id)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var exam = await response.Content.ReadFromJsonAsync<PracticeExamDto>();
        Assert.Equal($"api/practice-exams/{exam!.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Algorithmen", exam.SourceName);
        Assert.Equal(PracticeExamLevel.University, exam.Level);
        Assert.Equal("claude-sonnet-5-5", exam.Model);
        Assert.Equal(2, exam.TaskCount);
        Assert.Equal(7, exam.TotalPoints);
        Assert.Equal("Probeklausur Algorithmen · Universität · 09.10.2026", exam.Title);
    }

    [Fact]
    public async Task GetModels_ReturnsTheConfiguredModels()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var models = await client.GetFromJsonAsync<List<AiModelDto>>("api/practice-exams/models");

        Assert.Contains(models!, m => m.Id == "claude-sonnet-5-5" && m.IsDefault);
        Assert.Contains(models!, m => m.Id == "claude-opus-5-5");
    }

    [Fact]
    public async Task GenerateStartSaveSubmitGrade_CompletesTheAttempt()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);

        var sheet = await StartAsync(client, exam.Id, withTimeLimit: true);
        var singleChoice = sheet.Tasks.Single(t => t.Kind == PracticeExamTaskKind.SingleChoice);
        var open = sheet.Tasks.Single(t => t.Kind == PracticeExamTaskKind.Open);
        var save = await client.PutAsJsonAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/answers/{open.TaskId}",
            new SavePracticeExamAnswerRequest(null, "Dijkstra wählt gierig.")
        );
        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);

        var reloaded = await client.GetFromJsonAsync<PracticeExamSheetDto>(
            $"api/practice-exam-attempts/{sheet.AttemptId}/sheet"
        );
        Assert.Equal(
            "Dijkstra wählt gierig.",
            reloaded!.Tasks.Single(t => t.TaskId == open.TaskId).AnswerText
        );
        Assert.Equal(Now.AddMinutes(60), reloaded.DueAt);

        var submitResponse = await client.PostAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/submit",
            content: null
        );
        submitResponse.EnsureSuccessStatusCode();
        var review = (await submitResponse.Content.ReadFromJsonAsync<PracticeExamReviewDto>())!;
        Assert.Equal(PracticeExamAttemptStatus.Submitted, review.Status);
        Assert.Equal(0, review.Tasks.Single(t => t.TaskId == singleChoice.TaskId).AwardedPoints);
        var openReview = review.Tasks.Single(t => t.TaskId == open.TaskId);
        Assert.Equal("Musterlösung.", openReview.Solution);
        Assert.Equal("Dijkstra", openReview.Source?.Title);

        var gradeResponse = await client.PutAsJsonAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/answers/{open.TaskId}/grading",
            new GradePracticeExamAnswerRequest([openReview.Criteria[1].Id])
        );
        gradeResponse.EnsureSuccessStatusCode();
        var graded = (await gradeResponse.Content.ReadFromJsonAsync<PracticeExamReviewDto>())!;
        Assert.Equal(PracticeExamAttemptStatus.Graded, graded.Status);
        Assert.Equal(3, graded.AwardedPoints);
        Assert.Equal(43, graded.Percent);

        var list = await client.GetFromJsonAsync<List<PracticeExamDto>>("api/practice-exams");
        var listed = Assert.Single(list!);
        Assert.Equal(1, listed.AttemptCount);
        Assert.Equal(3, listed.BestAttempt?.AwardedPoints);
        Assert.Null(listed.OpenAttemptId);
        var history = await client.GetFromJsonAsync<List<PracticeExamAttemptSummaryDto>>(
            $"api/practice-exams/{exam.Id}/attempts"
        );
        Assert.Equal(sheet.AttemptId, Assert.Single(history!).Id);
    }

    [Fact]
    public async Task Start_Twice_ResumesTheOpenAttempt()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);

        var first = await StartAsync(client, exam.Id);
        var second = await StartAsync(client, exam.Id, withTimeLimit: true);

        Assert.Equal(first.AttemptId, second.AttemptId);
        Assert.Null(second.DueAt);
        var cover = await client.GetFromJsonAsync<PracticeExamDto>($"api/practice-exams/{exam.Id}");
        Assert.Equal(first.AttemptId, cover!.OpenAttemptId);
    }

    [Fact]
    public async Task SaveAnswer_AfterTheTimeLimit_Returns409TimeOver()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);
        var sheet = await StartAsync(client, exam.Id, withTimeLimit: true);
        _timeProvider.UtcNow = Now.AddMinutes(62);

        var response = await client.PutAsJsonAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/answers/{sheet.Tasks[1].TaskId}",
            new SavePracticeExamAnswerRequest(null, "zu spät")
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamTimeOver,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task SheetSaveAndReview_RespectTheSubmission()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);
        var sheet = await StartAsync(client, exam.Id);

        var earlyReview = await client.GetAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/review"
        );
        await client.PostAsync($"api/practice-exam-attempts/{sheet.AttemptId}/submit", null);
        var lateSheet = await client.GetAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/sheet"
        );
        var lateSave = await client.PutAsJsonAsync(
            $"api/practice-exam-attempts/{sheet.AttemptId}/answers/{sheet.Tasks[1].TaskId}",
            new SavePracticeExamAnswerRequest(null, "zu spät")
        );

        Assert.Equal(HttpStatusCode.Conflict, earlyReview.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamAttemptNotSubmitted,
            await GetProblemValueAsync(earlyReview, "errorCode")
        );
        Assert.Equal(HttpStatusCode.Conflict, lateSheet.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamAttemptSubmitted,
            await GetProblemValueAsync(lateSave, "errorCode")
        );
    }

    [Fact]
    public async Task ArchivedExam_CannotBeStarted()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);
        await client.PostAsync($"api/practice-exams/{exam.Id}/archive", null);

        var response = await client.PostAsJsonAsync(
            $"api/practice-exams/{exam.Id}/attempts",
            new StartPracticeExamAttemptRequest(false)
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamArchived,
            await GetProblemValueAsync(response, "errorCode")
        );
        Assert.Empty((await client.GetFromJsonAsync<List<PracticeExamDto>>("api/practice-exams"))!);
    }

    [Fact]
    public async Task ExcludeTask_LeavesItOutOfTheNextAttempt()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var exam = await GenerateAsync(client);
        var first = await StartAsync(client, exam.Id);
        await client.PostAsync($"api/practice-exam-attempts/{first.AttemptId}/submit", null);

        var response = await client.PostAsync(
            $"api/practice-exams/tasks/{first.Tasks[0].TaskId}/exclude",
            null
        );
        var second = await StartAsync(client, exam.Id);

        var updated = await response.Content.ReadFromJsonAsync<PracticeExamDto>();
        Assert.Equal(1, updated!.TaskCount);
        Assert.Equal(1, updated.ExcludedTaskCount);
        Assert.Equal(first.Tasks[1].TaskId, Assert.Single(second.Tasks).TaskId);
    }

    [Fact]
    public async Task Generate_WithInvalidDuration_Returns400()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();
        var course = await CreateCourseWithNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(course.Id, durationMinutes: 50)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamValidationFailed,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task Generate_WithUnknownCourse_Returns404WithCourseErrorCode()
    {
        using var factory = CreateFactory(FakeGenerator());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(Guid.NewGuid())
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            CourseErrorCodes.CourseNotFound,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task GetById_WithUnknownExam_Returns404()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"api/practice-exams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            PracticeExamErrorCodes.PracticeExamNotFound,
            await GetProblemValueAsync(response, "errorCode")
        );
    }

    [Fact]
    public async Task Generate_WhenGenerationFails_Returns502WithReason()
    {
        var generator = new Mock<IPracticeExamGenerator>();
        generator
            .Setup(g =>
                g.GenerateAsync(
                    It.IsAny<PracticeExamGenerationInput>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(
                new AiGenerationFailedException(AiGenerationFailureReason.Truncated, "cut")
            );
        using var factory = CreateFactory(generator.Object);
        using var client = factory.CreateClient();
        var course = await CreateCourseWithNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(course.Id)
        );

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(
            AiErrorCodes.AiGenerationFailed,
            ((JsonElement)problemDetails!.Extensions["errorCode"]!).GetString()
        );
        Assert.Equal(
            nameof(AiGenerationFailureReason.Truncated),
            ((JsonElement)problemDetails.Extensions["reason"]!).GetString()
        );
    }

    [Fact]
    public async Task Generate_WithoutApiKey_Returns503AiNotConfigured()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var course = await CreateCourseWithNoteAsync(client);

        var response = await client.PostAsJsonAsync(
            "api/practice-exams/generate",
            Request(course.Id)
        );

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(
            AiErrorCodes.AiNotConfigured,
            await GetProblemValueAsync(response, "errorCode")
        );
    }
}
