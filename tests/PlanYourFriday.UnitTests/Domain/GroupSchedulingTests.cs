using PlanYourFriday.Domain.Groups;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class GroupSchedulingTests
{
    private static readonly Guid Organizer = Guid.NewGuid();
    private static readonly Guid Max = Guid.NewGuid();
    private static readonly Guid Tania = Guid.NewGuid();

    private static Group CreateGroup(int eventHours = 2)
    {
        var group = Group.Create(Organizer, "Friday", null, Range(2, 9, 23), TimeSpan.FromHours(eventHours), null, At(1, 0));
        group.AddMember(Max);
        group.AddMember(Tania);
        return group;
    }

    [Fact]
    public void FindCommonSlots_IntersectsEveryMembersSlotsAndDropsShortOnes()
    {
        var group = CreateGroup();
        group.SetAvailability(Organizer, [Range(2, 10, 20)], At(1, 0));
        group.SetAvailability(Max, [Range(2, 9, 12), Range(2, 14, 22)], At(1, 0));
        group.SetAvailability(Tania, [Range(2, 11, 13), Range(2, 15, 18)], At(1, 0));

        Assert.Equal([Range(2, 15, 18)], group.FindCommonSlots());
    }

    [Fact]
    public void FindCommonSlots_IsEmptyWhileSomeoneHasNoSlots()
    {
        var group = CreateGroup();
        group.SetAvailability(Organizer, [Range(2, 10, 20)], At(1, 0));

        Assert.Empty(group.FindCommonSlots());
    }

    [Fact]
    public void SuggestTimes_PrefersTimesMostMembersCanMakeWithoutOverlaps()
    {
        var group = CreateGroup();
        group.SetAvailability(Organizer, [Range(2, 10, 14)], At(1, 0));
        group.SetAvailability(Max, [Range(2, 10, 12), Range(2, 18, 20)], At(1, 0));
        group.SetAvailability(Tania, [Range(2, 10, 12)], At(1, 0));

        var suggestions = group.SuggestTimes(At(1, 0));

        Assert.Equal(Range(2, 10, 12), suggestions[0].Time);
        Assert.Equal(3, suggestions[0].FreeUserIds.Count);
        Assert.All(suggestions.Skip(1), s => Assert.False(s.Time.Overlaps(suggestions[0].Time)));
        Assert.Contains(suggestions, s => s.Time == Range(2, 18, 20) && s.FreeUserIds.SequenceEqual([Max]));
    }

    [Fact]
    public void SuggestTimes_ReturnsAtMostRequestedCount()
    {
        var group = CreateGroup(eventHours: 1);
        group.SetAvailability(Organizer, [Range(2, 9, 23)], At(1, 0));

        Assert.Equal(3, group.SuggestTimes(At(1, 0), max: 3).Count);
        Assert.Empty(CreateGroup().SuggestTimes(At(1, 0)));
    }

    [Fact]
    public void SuggestTimes_SkipsTimesThatAlreadyStarted()
    {
        var group = CreateGroup();
        group.SetAvailability(Organizer, [Range(2, 9, 23)], At(1, 0));
        var now = At(2, 15).AddMinutes(10);

        var suggestions = group.SuggestTimes(now);

        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, s => Assert.True(s.Time.Start >= now));
        Assert.Equal(At(2, 15).AddMinutes(30), suggestions.Min(s => s.Time.Start));
    }
}
