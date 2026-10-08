using StudyHub.Data.Contract;
using StudyHub.Logic.Business.Contract;
using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Logic.Business;

public sealed class StudySessionOrchestrator(
    IStudySessionRepository sessionRepository,
    IStudySessionLifecycle sessionLifecycle,
    ICourseRepository courseRepository,
    ISemesterRepository semesterRepository
) : IStudySessionOrchestrator
{
    public async Task<StudySessionDto> CreateAsync(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var session = sessionLifecycle.Create(
            request.Title,
            request.CourseId,
            request.SemesterId,
            request.Date,
            request.StartTime,
            request.DurationMinutes,
            request.Location);

        var course = await GetAssignableCourseAsync(session.CourseId, currentCourseId: null, cancellationToken);
        var semester = await GetAssignableSemesterAsync(session.SemesterId, currentSemesterId: null, cancellationToken);

        await sessionRepository.AddAsync(session, cancellationToken);
        await sessionRepository.SaveChangesAsync(cancellationToken);

        return StudySessionMapper.ToDto(session, course, semester);
    }

    public async Task<StudySessionDto> UpdateAsync(
        UpdateStudySessionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var session = await GetExistingSessionAsync(request.Id, cancellationToken);
        var updated = sessionLifecycle.Update(
            session,
            request.Title,
            request.CourseId,
            request.SemesterId,
            request.Date,
            request.StartTime,
            request.DurationMinutes,
            request.Location);

        var course = await GetAssignableCourseAsync(updated.CourseId, session.CourseId, cancellationToken);
        var semester = await GetAssignableSemesterAsync(updated.SemesterId, session.SemesterId, cancellationToken);

        sessionRepository.Update(updated);
        await sessionRepository.SaveChangesAsync(cancellationToken);

        return StudySessionMapper.ToDto(updated, course, semester);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await GetExistingSessionAsync(id, cancellationToken);

        sessionRepository.Remove(session);
        await sessionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<StudySession> GetExistingSessionAsync(Guid id, CancellationToken cancellationToken) =>
        await sessionRepository.GetByIdAsync(id, cancellationToken) ?? throw new StudySessionNotFoundException(id);

    // An archived course cannot be newly linked; a session already linked to it may keep the link.
    private async Task<Course?> GetAssignableCourseAsync(Guid? courseId, Guid? currentCourseId, CancellationToken cancellationToken)
    {
        if (courseId is not { } id)
        {
            return null;
        }

        var course = await courseRepository.GetByIdAsync(id, cancellationToken) ?? throw new CourseNotFoundException(id);
        if (course.IsArchived && id != currentCourseId)
        {
            throw new CourseArchivedException(id);
        }

        return course;
    }

    // An archived semester cannot be newly linked; a session already linked to it may keep the link.
    private async Task<Semester?> GetAssignableSemesterAsync(Guid? semesterId, Guid? currentSemesterId, CancellationToken cancellationToken)
    {
        if (semesterId is not { } id)
        {
            return null;
        }

        var semester = await semesterRepository.GetByIdAsync(id, cancellationToken) ?? throw new SemesterNotFoundException(id);
        if (semester.IsArchived && id != currentSemesterId)
        {
            throw new SemesterArchivedException(id);
        }

        return semester;
    }
}
