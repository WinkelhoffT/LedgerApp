using StudyHub.Shared.PracticeExams;

namespace StudyHub.Logic.Domain.Contract;

/// <summary>Domain rules for creating and changing a stored practice exam.</summary>
public interface IPracticeExamLifecycle
{
    /// <summary>
    /// Creates the exam from validated tasks: a title from source, level and date, points from the
    /// criteria, source references mapped back to notes or cards, and the options of every
    /// single-choice task shuffled once.
    /// </summary>
    StoredPracticeExam Create(
        GeneratePracticeExamRequest request,
        string model,
        string promptVersion,
        PracticeExamMaterial material,
        IReadOnlyList<GeneratedPracticeExamTask> tasks
    );

    PracticeExam Archive(PracticeExam exam);

    PracticeExam Restore(PracticeExam exam);

    /// <summary>Leaves a flawed task out of future attempts; attempts already started keep it.</summary>
    /// <exception cref="PracticeExamArchivedException">The exam is archived.</exception>
    PracticeExamTask ExcludeTask(PracticeExam exam, PracticeExamTask task);
}
