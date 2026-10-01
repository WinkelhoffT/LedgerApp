using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Logic.Business.Courses;

public class CourseOrchestratorTests
{
    private static readonly SemesterLifecycle SemesterLifecycle = new();
    private static readonly CourseLifecycle CourseLifecycle = new();

    private static readonly Guid SemesterId = Guid.NewGuid();

    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly Mock<ISemesterRepository> _semesterRepository = new();
    private readonly CourseOrchestrator _sut;

    public CourseOrchestratorTests()
    {
        _sut = new CourseOrchestrator(_courseRepository.Object, _semesterRepository.Object, CourseLifecycle);

        _semesterRepository.Setup(r => r.GetByIdAsync(SemesterId, default))
            .ReturnsAsync(SemesterLifecycle.Create("Winter 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public async Task CreateAsync_WithUniqueName_AddsCourseAndReturnsDto()
    {
        _courseRepository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CreateCourseRequest("Algorithms", "Description", "#2563eb", SemesterId));

        Assert.Equal("Algorithms", result.Name);
        Assert.Equal(SemesterId, result.SemesterId);
        _courseRepository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Once);
        _courseRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsDuplicateCourseNameException()
    {
        _courseRepository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateCourseNameException>(
            () => _sut.CreateAsync(new CreateCourseRequest("Algorithms", null, "#2563eb", SemesterId)));

        _courseRepository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSemesterNotFound_ThrowsSemesterNotFoundException()
    {
        var unknownSemesterId = Guid.NewGuid();
        _courseRepository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(false);
        _semesterRepository.Setup(r => r.GetByIdAsync(unknownSemesterId, default))
            .ReturnsAsync((Semester?)null);

        await Assert.ThrowsAsync<SemesterNotFoundException>(
            () => _sut.CreateAsync(new CreateCourseRequest("Algorithms", null, "#2563eb", unknownSemesterId)));

        _courseRepository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSemesterArchived_ThrowsSemesterArchivedException()
    {
        var archivedSemester = SemesterLifecycle.Create("Winter 2025/26", new DateOnly(2025, 10, 1), new DateOnly(2026, 3, 31));
        archivedSemester = SemesterLifecycle.Archive(archivedSemester);

        _courseRepository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(false);
        _semesterRepository.Setup(r => r.GetByIdAsync(archivedSemester.Id, default))
            .ReturnsAsync(archivedSemester);

        await Assert.ThrowsAsync<SemesterArchivedException>(
            () => _sut.CreateAsync(new CreateCourseRequest("Algorithms", null, "#2563eb", archivedSemester.Id)));

        _courseRepository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var id = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(
            () => _sut.UpdateAsync(new UpdateCourseRequest(id, "New Name", null, "#2563eb", SemesterId)));
    }

    [Fact]
    public async Task UpdateAsync_WhenCourseArchived_ThrowsCourseArchivedException()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        course = CourseLifecycle.Archive(course);

        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);
        _courseRepository.Setup(r => r.ExistsByNameAsync("New Name", course.Id, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<CourseArchivedException>(
            () => _sut.UpdateAsync(new UpdateCourseRequest(course.Id, "New Name", null, "#2563eb", SemesterId)));
    }

    [Fact]
    public async Task UpdateAsync_WhenSemesterArchived_ThrowsSemesterArchivedException()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        var archivedSemester = SemesterLifecycle.Create("Summer 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30));
        archivedSemester = SemesterLifecycle.Archive(archivedSemester);

        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);
        _courseRepository.Setup(r => r.ExistsByNameAsync("Algorithms", course.Id, default)).ReturnsAsync(false);
        _semesterRepository.Setup(r => r.GetByIdAsync(archivedSemester.Id, default)).ReturnsAsync(archivedSemester);

        await Assert.ThrowsAsync<SemesterArchivedException>(
            () => _sut.UpdateAsync(new UpdateCourseRequest(course.Id, "Algorithms", null, "#2563eb", archivedSemester.Id)));
    }

    [Fact]
    public async Task ArchiveAsync_SetsCourseArchivedAndSaves()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);

        var result = await _sut.ArchiveAsync(course.Id);

        Assert.True(result.IsArchived);
        _courseRepository.Verify(r => r.Update(It.Is<Course>(c => c.IsArchived)), Times.Once);
        _courseRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsCourseNotArchivedAndSaves()
    {
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", SemesterId);
        course = CourseLifecycle.Archive(course);
        _courseRepository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);

        var result = await _sut.RestoreAsync(course.Id);

        Assert.False(result.IsArchived);
        _courseRepository.Verify(r => r.Update(It.Is<Course>(c => !c.IsArchived)), Times.Once);
        _courseRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var id = Guid.NewGuid();
        _courseRepository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(() => _sut.ArchiveAsync(id));
    }
}
