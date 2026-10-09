namespace StudyHub.Logic.Domain.Contract;

/// <summary>
/// Turns study activities into study time per study day. A completed session counts its actual
/// duration, a flashcard answer the time since the previous answer (at most a minute, as Anki's
/// "maximum answer seconds"), and a submitted practice exam attempt its time from start to
/// submission (at most twice the exam's duration). Time covered by several activities counts once,
/// for the activity that started first; a completed session wins a tie.
/// </summary>
public interface IStudyTimeProcessor
{
    /// <summary>The study days with study time or answers, ordered by date.</summary>
    IReadOnlyList<StudyTimeDay> GetDays(StudyActivities activities);

    /// <summary>
    /// 0 = no study time, 1 = under 30 minutes, 2 = under 1 hour, 3 = under 2 hours, 4 = 2 hours or more.
    /// </summary>
    int GetHeatLevel(int minutes);
}
