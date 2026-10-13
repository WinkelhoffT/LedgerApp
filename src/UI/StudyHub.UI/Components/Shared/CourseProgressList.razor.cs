using Microsoft.AspNetCore.Components;
using StudyHub.Shared.Analytics;

namespace StudyHub.UI.Components.Shared;

/// <summary>
/// The active semester's courses with their flashcard buckets, practice exam results and study
/// time, shown side by side rather than as one combined percentage.
/// </summary>
public partial class CourseProgressList
{
    /// <summary>The overview; <c>null</c> while it is loading.</summary>
    [Parameter]
    public CourseProgressOverviewDto? Overview { get; set; }
}
