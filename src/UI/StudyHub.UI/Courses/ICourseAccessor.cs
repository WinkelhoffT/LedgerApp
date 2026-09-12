using StudyHub.Logic.Business.Courses;

namespace StudyHub.UI.Courses;

/// <summary>
/// Narrow HTTP access to StudyHub.Api's course endpoints, covering only what the Courses pages
/// actually call - not the full <see cref="ICourseManagement"/> business contract.
/// </summary>
public interface ICourseAccessor
{
    Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default);

    Task<CourseDto> UpdateAsync(UpdateCourseRequest request, CancellationToken cancellationToken = default);

    Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
