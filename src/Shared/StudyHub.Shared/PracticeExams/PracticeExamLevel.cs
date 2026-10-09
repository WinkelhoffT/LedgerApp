namespace StudyHub.Shared.PracticeExams;

/// <summary>
/// How a practice exam asks about the material. The level changes the style and depth of the
/// tasks, not the topics.
/// </summary>
public enum PracticeExamLevel
{
    /// <summary>Intermediate secondary level ("Oberschule", Mittlerer Schulabschluss).</summary>
    SecondarySchool,

    /// <summary>Upper secondary level ("Gymnasium", Abitur).</summary>
    Gymnasium,

    /// <summary>Bachelor exam ("Universität").</summary>
    University,
}
