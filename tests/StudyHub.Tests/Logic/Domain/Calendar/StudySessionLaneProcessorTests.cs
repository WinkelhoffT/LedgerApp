using StudyHub.Logic.Domain;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Tests.Logic.Domain.Calendar;

public class StudySessionLaneProcessorTests
{
    private readonly StudySessionLaneProcessor _sut = new();

    private static StudySession Session(string title, int hour, int minute, int durationMinutes) =>
        new(Guid.NewGuid(), title, null, null, new DateOnly(2026, 10, 8), new TimeOnly(hour, minute), durationMinutes, null, DateTime.UtcNow, DateTime.UtcNow);

    private (string Title, int Lane, int LaneCount)[] Assign(params StudySession[] sessions) =>
        _sut.Assign(sessions).Select(l => (l.Session.Title, l.Lane, l.LaneCount)).ToArray();

    [Fact]
    public void Assign_WithoutSessions_ReturnsNothing()
    {
        Assert.Empty(_sut.Assign([]));
    }

    [Fact]
    public void Assign_WithoutOverlap_UsesOneLaneOrderedByStart()
    {
        var result = Assign(Session("B", 14, 0, 60), Session("A", 9, 0, 60));

        Assert.Equal([("A", 0, 1), ("B", 0, 1)], result);
    }

    [Fact]
    public void Assign_SessionsThatOnlyTouch_DoNotOverlap()
    {
        var result = Assign(Session("A", 9, 0, 60), Session("B", 10, 0, 60));

        Assert.Equal([("A", 0, 1), ("B", 0, 1)], result);
    }

    [Fact]
    public void Assign_TwoOverlappingSessions_SitSideBySide()
    {
        var result = Assign(Session("A", 9, 0, 60), Session("B", 9, 30, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2)], result);
    }

    [Fact]
    public void Assign_ChainWhoseEndsDoNotOverlap_ReusesTheFirstLane()
    {
        var result = Assign(Session("A", 9, 0, 60), Session("B", 9, 30, 60), Session("C", 10, 0, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2), ("C", 0, 2)], result);
    }

    [Fact]
    public void Assign_IdenticalStart_PutsTheLongerSessionFirst()
    {
        var result = Assign(Session("Short", 9, 0, 30), Session("Long", 9, 0, 90));

        Assert.Equal([("Long", 0, 2), ("Short", 1, 2)], result);
    }

    [Fact]
    public void Assign_SeparateGroups_CountTheirLanesIndependently()
    {
        var result = Assign(
            Session("A", 9, 0, 120),
            Session("B", 9, 30, 30),
            Session("C", 10, 0, 30),
            Session("D", 14, 0, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2), ("C", 1, 2), ("D", 0, 1)], result);
    }

    [Fact]
    public void Assign_ThreeSessionsAtOnce_UseThreeLanes()
    {
        var result = Assign(Session("A", 9, 0, 60), Session("B", 9, 15, 60), Session("C", 9, 30, 60));

        Assert.Equal([("A", 0, 3), ("B", 1, 3), ("C", 2, 3)], result);
    }
}
