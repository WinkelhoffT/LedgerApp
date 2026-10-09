namespace StudyHub.Shared.PracticeExams;

public enum PracticeExamTaskKind
{
    /// <summary>Four options, exactly one correct; graded automatically on submission.</summary>
    SingleChoice,

    /// <summary>A free-text answer, graded by the student against a rubric.</summary>
    Open,
}
