using Moq;
using StudyHub.Logic.Business.Semesters;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Tests.Logic.Business.Semesters;

public class SemesterManagementTests
{
    private static readonly DateOnly StartDate = new(2025, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 3, 31);

    private readonly Mock<ISemesterRepository> _repository = new();
    private readonly SemesterManagement _sut;

    public SemesterManagementTests()
    {
        _sut = new SemesterManagement(_repository.Object);
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
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        semester.Archive();

        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);
        _repository.Setup(r => r.ExistsByNameAsync("New Name", semester.Id, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<SemesterArchivedException>(
            () => _sut.UpdateAsync(new UpdateSemesterRequest(semester.Id, "New Name", StartDate, EndDate)));
    }

    [Fact]
    public async Task ArchiveAsync_SetsSemesterArchivedAndSaves()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);

        var result = await _sut.ArchiveAsync(semester.Id);

        Assert.True(result.IsArchived);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_SetsSemesterNotArchivedAndSaves()
    {
        var semester = Semester.Create("Winter 2025/26", StartDate, EndDate);
        semester.Archive();
        _repository.Setup(r => r.GetByIdAsync(semester.Id, default)).ReturnsAsync(semester);

        var result = await _sut.RestoreAsync(semester.Id);

        Assert.False(result.IsArchived);
        _repository.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_WhenSemesterNotFound_ThrowsSemesterNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Semester?)null);

        await Assert.ThrowsAsync<SemesterNotFoundException>(() => _sut.ArchiveAsync(id));
    }
}
