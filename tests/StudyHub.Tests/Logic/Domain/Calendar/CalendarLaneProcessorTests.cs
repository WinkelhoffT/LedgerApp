using StudyHub.Logic.Domain;
using StudyHub.Logic.Domain.Contract;

namespace StudyHub.Tests.Logic.Domain.Calendar;

public class CalendarLaneProcessorTests
{
    private readonly CalendarLaneProcessor _sut = new();
    private readonly Dictionary<Guid, string> _names = [];

    private CalendarTimeSlot Slot(string name, int hour, int minute, int durationMinutes)
    {
        var slot = new CalendarTimeSlot(Guid.NewGuid(), new TimeOnly(hour, minute), durationMinutes);
        _names[slot.Id] = name;
        return slot;
    }

    private (string Name, int Lane, int LaneCount)[] Assign(params CalendarTimeSlot[] slots) =>
        _sut.Assign(slots).Select(l => (_names[l.Id], l.Lane, l.LaneCount)).ToArray();

    [Fact]
    public void Assign_WithoutSlots_ReturnsNothing()
    {
        Assert.Empty(_sut.Assign([]));
    }

    [Fact]
    public void Assign_WithoutOverlap_UsesOneLaneOrderedByStart()
    {
        var result = Assign(Slot("B", 14, 0, 60), Slot("A", 9, 0, 60));

        Assert.Equal([("A", 0, 1), ("B", 0, 1)], result);
    }

    [Fact]
    public void Assign_SlotsThatOnlyTouch_DoNotOverlap()
    {
        var result = Assign(Slot("A", 9, 0, 60), Slot("B", 10, 0, 60));

        Assert.Equal([("A", 0, 1), ("B", 0, 1)], result);
    }

    [Fact]
    public void Assign_TwoOverlappingSlots_SitSideBySide()
    {
        var result = Assign(Slot("A", 9, 0, 60), Slot("B", 9, 30, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2)], result);
    }

    [Fact]
    public void Assign_ChainWhoseEndsDoNotOverlap_ReusesTheFirstLane()
    {
        var result = Assign(Slot("A", 9, 0, 60), Slot("B", 9, 30, 60), Slot("C", 10, 0, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2), ("C", 0, 2)], result);
    }

    [Fact]
    public void Assign_IdenticalStart_PutsTheLongerSlotFirst()
    {
        var result = Assign(Slot("Short", 9, 0, 30), Slot("Long", 9, 0, 90));

        Assert.Equal([("Long", 0, 2), ("Short", 1, 2)], result);
    }

    [Fact]
    public void Assign_SeparateGroups_CountTheirLanesIndependently()
    {
        var result = Assign(
            Slot("A", 9, 0, 120),
            Slot("B", 9, 30, 30),
            Slot("C", 10, 0, 30),
            Slot("D", 14, 0, 60));

        Assert.Equal([("A", 0, 2), ("B", 1, 2), ("C", 1, 2), ("D", 0, 1)], result);
    }

    [Fact]
    public void Assign_ThreeSlotsAtOnce_UseThreeLanes()
    {
        var result = Assign(Slot("A", 9, 0, 60), Slot("B", 9, 15, 60), Slot("C", 9, 30, 60));

        Assert.Equal([("A", 0, 3), ("B", 1, 3), ("C", 2, 3)], result);
    }
}
