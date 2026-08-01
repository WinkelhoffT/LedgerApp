using Moq;
using StudyHub.Logic.Business.Courses;
using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Tests.Logic.Business.Courses;

public class CourseManagementTests
{
    private readonly Mock<ICourseRepository> _repository = new();
    private readonly CourseManagement _sut;

    public CourseManagementTests()
    {
        _sut = new CourseManagement(_repository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithUniqueName_AddsCourseAndReturnsDto()
    {
        _repository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CreateCourseRequest("Algorithms", "Description", "#2563eb"));

        Assert.Equal("Algorithms", result.Name);
        _repository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ThrowsDuplicateCourseNameException()
    {
        _repository.Setup(r => r.ExistsByNameAsync("Algorithms", null, default))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateCourseNameException>(
            () => _sut.CreateAsync(new CreateCourseRequest("Algorithms", null, "#2563eb")));

        _repository.Verify(r => r.AddAsync(It.IsAny<Course>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(
            () => _sut.UpdateAsync(new UpdateCourseRequest(id, "New Name", null, "#2563eb")));
    }

    [Fact]
    public async Task UpdateAsync_WhenCourseArchived_ThrowsCourseArchivedException()
    {
        var course = Course.Create("Algorithms", null, "#2563eb");
        course.Archive();

        _repository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);
        _repository.Setup(r => r.ExistsByNameAsync("New Name", course.Id, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<CourseArchivedException>(
            () => _sut.UpdateAsync(new UpdateCourseRequest(course.Id, "New Name", null, "#2563eb")));
    }

    [Fact]
    public async Task ArchiveAsync_SetsCourseArchivedAndSaves()
    {
        var course = Course.Create("Algorithms", null, "#2563eb");
        _repository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);

        var result = await _sut.ArchiveAsync(course.Id);

        Assert.True(result.IsArchived);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsCourseNotArchivedAndSaves()
    {
        var course = Course.Create("Algorithms", null, "#2563eb");
        course.Archive();
        _repository.Setup(r => r.GetByIdAsync(course.Id, default)).ReturnsAsync(course);

        var result = await _sut.RestoreAsync(course.Id);

        Assert.False(result.IsArchived);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_WhenCourseNotFound_ThrowsCourseNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<CourseNotFoundException>(() => _sut.ArchiveAsync(id));
    }
}
