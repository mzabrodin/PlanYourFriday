using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Polls;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests;

public class AccessTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Friend = Guid.NewGuid();

    private static Group CreateGroup()
    {
        var group = Group.Create(Owner, "Board games", null, Range(2, 10, 22), TimeSpan.FromHours(2), null, At(1, 0));
        group.AddMember(Friend);
        return group;
    }

    private static Poll CreatePoll()
    {
        return Poll.Create(Owner, "What on Saturday?", null, [Friend], ["Bowling", "Karaoke"], null, false, true, At(1, 0));
    }

    [Fact]
    public void EnsureMember_ReturnsGroupForMember()
    {
        var group = CreateGroup();

        Assert.Same(group, group.EnsureMember(Friend));
    }

    [Fact]
    public void EnsureMember_HidesMissingGroupAndGroupOfOthers()
    {
        Group? missing = null;

        var notFound = Assert.Throws<EntityNotFoundException>(() => missing.EnsureMember(Friend));
        var notMember = Assert.Throws<EntityNotFoundException>(() => CreateGroup().EnsureMember(Guid.NewGuid()));

        Assert.Equal("group.not_found", notFound.Code);
        Assert.Equal("group.not_found", notMember.Code);
    }

    [Fact]
    public void EnsureParticipant_ReturnsPollForParticipant()
    {
        var poll = CreatePoll();

        Assert.Same(poll, poll.EnsureParticipant(Friend));
    }

    [Fact]
    public void EnsureParticipant_HidesMissingPollAndPollOfOthers()
    {
        Poll? missing = null;

        var notFound = Assert.Throws<EntityNotFoundException>(() => missing.EnsureParticipant(Friend));
        var notParticipant = Assert.Throws<EntityNotFoundException>(() => CreatePoll().EnsureParticipant(Guid.NewGuid()));

        Assert.Equal("poll.not_found", notFound.Code);
        Assert.Equal("poll.not_found", notParticipant.Code);
    }
}
