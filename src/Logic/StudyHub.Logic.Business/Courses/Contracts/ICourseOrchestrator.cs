using StudyHub.Shared.Courses;

namespace StudyHub.Logic.Business.Courses;

public interface ICourseOrchestrator
{
    Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CourseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);

    Task<CourseDto> UpdateAsync(UpdateCourseRequest request, CancellationToken cancellationToken = default);

    Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
