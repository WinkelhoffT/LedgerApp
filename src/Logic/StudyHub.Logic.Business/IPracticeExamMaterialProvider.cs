using StudyHub.Shared.Courses;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Business;

/// <summary>Loads and checks the material a practice exam is generated from.</summary>
public interface IPracticeExamMaterialProvider
{
    /// <exception cref="PracticeExamValidationException">No source chosen, a note that cannot be used, or the material is over the limit.</exception>
    /// <exception cref="CourseNotFoundException">The course does not exist.</exception>
    /// <exception cref="CourseArchivedException">The course is archived.</exception>
    /// <exception cref="FlashcardDeckNotFoundException">The deck does not exist.</exception>
    /// <exception cref="FlashcardDeckArchivedException">The deck is archived.</exception>
    Task<PracticeExamMaterial> GetMaterialAsync(
        GeneratePracticeExamRequest request,
        CancellationToken cancellationToken = default
    );
}
