using Moq;
using StudyHub.Data.Contract;
using StudyHub.Logic.Business;
using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;

namespace StudyHub.Tests.Logic.Business.Semesters;

public class SemesterOrchestratorTests
{
    private static readonly SemesterLifecycle SemesterLifecycle = new();
    private static readonly CourseLifecycle CourseLifecycle = new();

    private static readonly DateOnly StartDate = new(2025, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 3, 31);

    private readonly Mock<ISemesterRepository> _repository = new();
    private readonly Mock<ICourseRepository> _courseRepository = new();
    private readonly SemesterOrchestrator _sut;

    public SemesterOrchestratorTests()
    {
        _sut = new SemesterOrchestrator(_repository.Object, _courseRepository.Object, SemesterLifecycle, CourseLifecycle);

        _courseRepository.Setup(r => r.GetBySemesterIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task CreateAsync_WithUniqueName_AddsSemesterAndReturnsDto()
    {
        _repository.Setup(r => r.ExistsByNameAsync("Winter 2025/26", null, default))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate));

        Assert.Equal("Winter 2025/26", result.Name);
        _repository.Verify(r => r.AddAsync(It.IsAny<Semester>(), default), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsDuplicateSemesterNameException()
    {
        _repository.Setup(r => r.ExistsByNameAsync("Winter 2025/26", null, default))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateSemesterNameException>(
            () => _sut.CreateAsync(new CreateSemesterRequest("Winter 2025/26", StartDate, EndDate)));

        _repository.Verify(r => r.AddAsync(It.IsAny<Semester>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSemesterNotFound_ThrowsSemesterNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Semester?)null);

        await Assert.ThrowsAsync<SemesterNotFoundException>(
            () => _sut.UpdateAsync(new UpdateSemesterRequest(id, "New Name", StartDate, EndDate)));
    }

    [Fact]
    public async Task UpdateAsync_WhenSemesterArchived_ThrowsSemesterArchivedException()
    {
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
        semester = SemesterLifecycle.Archive(semester);

        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);
        _repository.Setup(r => r.ExistsByNameAsync("New Name", semester.Id, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<SemesterArchivedException>(
            () => _sut.UpdateAsync(new UpdateSemesterRequest(semester.Id, "New Name", StartDate, EndDate)));
    }

    [Fact]
    public async Task ArchiveAsync_SetsSemesterArchivedAndSaves()
    {
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);

        var result = await _sut.ArchiveAsync(semester.Id);

        Assert.True(result.IsArchived);
        _repository.Verify(r => r.Update(It.Is<Semester>(s => s.IsArchived)), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsSemesterNotArchivedAndSaves()
    {
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
        semester = SemesterLifecycle.Archive(semester);
        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);

        var result = await _sut.RestoreAsync(semester.Id);

        Assert.False(result.IsArchived);
        _repository.Verify(r => r.Update(It.Is<Semester>(s => !s.IsArchived)), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_AlsoArchivesAllCoursesInSemester()
    {
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
        var course1 = CourseLifecycle.Create("Algorithms", null, "#2563eb", semester.Id);
        var course2 = CourseLifecycle.Create("Databases", null, "#16a34a", semester.Id);

        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);
        _courseRepository.Setup(r => r.GetBySemesterIdAsync(semester.Id, default))
            .ReturnsAsync([course1, course2]);

        await _sut.ArchiveAsync(semester.Id);

        _courseRepository.Verify(r => r.Update(It.Is<Course>(c => c.Id == course1.Id && c.IsArchived)), Times.Once);
        _courseRepository.Verify(r => r.Update(It.Is<Course>(c => c.Id == course2.Id && c.IsArchived)), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_DoesNotRestoreCourses()
    {
        var semester = SemesterLifecycle.Create("Winter 2025/26", StartDate, EndDate);
        semester = SemesterLifecycle.Archive(semester);
        var course = CourseLifecycle.Create("Algorithms", null, "#2563eb", semester.Id);
        course = CourseLifecycle.Archive(course);

        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);

        await _sut.RestoreAsync(semester.Id);

        _courseRepository.Verify(r => r.Update(It.IsAny<Course>()), Times.Never);
        _courseRepository.Verify(r => r.GetBySemesterIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task ArchiveAsync_WhenSemesterNotFound_ThrowsSemesterNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Semester?)null);

        await Assert.ThrowsAsync<SemesterNotFoundException>(() => _sut.ArchiveAsync(id));
    }
}
