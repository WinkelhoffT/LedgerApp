using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Logic.Business.Courses;

public sealed class CourseManagement(ICourseRepository courseRepository) : ICourseManagement
{
    public async Task<IReadOnlyList<CourseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var courses = await courseRepository.GetAllAsync(cancellationToken);
        return courses.Select(ToDto).ToList();
    }

    public async Task<CourseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await GetExistingCourseAsync(id, cancellationToken);
        return ToDto(course);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludingId: null, cancellationToken);

        var course = Course.Create(request.Name, request.Description, request.Color);

        await courseRepository.AddAsync(course, cancellationToken);
        await courseRepository.SaveChangesAsync(cancellationToken);

        return ToDto(course);
    }

    public async Task<CourseDto> UpdateAsync(UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var course = await GetExistingCourseAsync(request.Id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, request.Id, cancellationToken);

        course.Update(request.Name, request.Description, request.Color);

        await courseRepository.SaveChangesAsync(cancellationToken);

        return ToDto(course);
    }

    public async Task<CourseDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await GetExistingCourseAsync(id, cancellationToken);

        course.Archive();

        await courseRepository.SaveChangesAsync(cancellationToken);

        return ToDto(course);
    }

    public async Task<CourseDto> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var course = await GetExistingCourseAsync(id, cancellationToken);

        course.Restore();

        await courseRepository.SaveChangesAsync(cancellationToken);

        return ToDto(course);
    }

    private async Task<Course> GetExistingCourseAsync(Guid id, CancellationToken cancellationToken) =>
        await courseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new CourseNotFoundException(id);

    private async Task EnsureNameIsUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await courseRepository.ExistsByNameAsync(name, excludingId, cancellationToken))
        {
            throw new DuplicateCourseNameException(name);
        }
    }

    private static CourseDto ToDto(Course course) => new(
        course.Id,
        course.Name,
        course.Description,
        course.Color,
        course.IsArchived,
        course.CreatedAt,
        course.UpdatedAt);
}
