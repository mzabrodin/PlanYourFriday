using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Polls;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class PollTests
{
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Friend = Guid.NewGuid();
    private static readonly DateTime Now = At(1, 0);

    private static Poll CreatePoll(bool multipleChoice = true, bool allowNewOptions = true, DateTime? deadline = null)
    {
        return Poll.Create(Creator, " What on Saturday? ", null, [Friend], ["Bowling", "Karaoke", "bowling", " "], deadline, multipleChoice, allowNewOptions, Now);
    }

    [Fact]
    public void Create_TrimsAndDeduplicatesOptions()
    {
        var poll = CreatePoll();

        Assert.Equal("What on Saturday?", poll.Title);
        Assert.Equal(["Bowling", "Karaoke"], poll.Options.Select(o => o.Title));
        Assert.Equal([0, 1], poll.Options.Select(o => o.Position));
        Assert.True(poll.IsParticipant(Creator));
        Assert.True(poll.IsParticipant(Friend));
    }

    [Fact]
    public void Create_RequiresAFriendAndTwoOptions()
    {
        Assert.Equal("poll.no_participants", Assert.Throws<BusinessRuleViolationException>(() =>
                Poll.Create(Creator, "Plans", null, [Creator], ["A", "B"], null, true, true, Now))
            .Code);
        Assert.Equal("poll.too_few_options", Assert.Throws<BusinessRuleViolationException>(() =>
                Poll.Create(Creator, "Plans", null, [Friend], ["A", "a"], null, true, true, Now))
            .Code);
    }

    [Fact]
    public void Vote_ReplacesPreviousChoices()
    {
        var poll = CreatePoll();
        var (bowling, karaoke) = (poll.Options.First(), poll.Options.Last());

        poll.Vote(Friend, [bowling.Id, karaoke.Id], Now);
        poll.Vote(Friend, [karaoke.Id], Now);

        Assert.Empty(bowling.Votes);
        Assert.Single(karaoke.Votes);
        Assert.True(poll.HasVoted(Friend));
    }

    [Fact]
    public void Vote_SingleChoiceAllowsOneOption()
    {
        var poll = CreatePoll(multipleChoice: false);

        Assert.Equal("poll.single_choice", Assert.Throws<BusinessRuleViolationException>(() =>
                poll.Vote(Friend, [.. poll.Options.Select(o => o.Id)], Now))
            .Code);
    }

    [Fact]
    public void Vote_RejectsOutsidersAndClosedPolls()
    {
        var poll = CreatePoll(deadline: At(2, 0));
        var option = poll.Options.First().Id;

        Assert.Throws<AccessDeniedException>(() => poll.Vote(Guid.NewGuid(), [option], Now));
        Assert.Equal("poll.closed", Assert.Throws<InvalidStateException>(() => poll.Vote(Friend, [option], At(2, 1))).Code);
    }

    [Fact]
    public void AddOption_RespectsLockAndDuplicates()
    {
        var locked = CreatePoll(allowNewOptions: false);
        Assert.Throws<AccessDeniedException>(() => locked.AddOption(Friend, "Cinema", Now));
        locked.AddOption(Creator, "Cinema", Now);

        var open = CreatePoll();
        open.AddOption(Friend, "Cinema", Now);
        Assert.Throws<AlreadyExistsException>(() => open.AddOption(Friend, "cinema", Now));
    }

    [Fact]
    public void Winners_AreKnownOnlyAfterClosingAndIncludeTies()
    {
        var poll = CreatePoll();
        var (bowling, karaoke) = (poll.Options.First(), poll.Options.Last());
        poll.Vote(Creator, [bowling.Id], Now);
        poll.Vote(Friend, [karaoke.Id], Now);

        Assert.Empty(poll.Winners(Now));
        Assert.Throws<AccessDeniedException>(() => poll.Close(Friend, Now));
        poll.Close(Creator, Now);

        Assert.Equal([bowling.Id, karaoke.Id], poll.Winners(Now).Select(o => o.Id));
    }

    [Fact]
    public void LinkGroup_RequiresClosedPollAndHappensOnce()
    {
        var poll = CreatePoll();
        Assert.Equal("poll.still_open", Assert.Throws<InvalidStateException>(() => poll.LinkGroup(Friend, Guid.NewGuid(), Now)).Code);

        poll.Close(Creator, Now);
        var groupId = Guid.NewGuid();
        poll.LinkGroup(Friend, groupId, Now);

        Assert.Equal(groupId, poll.GroupId);
        Assert.Equal("poll.already_planned", Assert.Throws<InvalidStateException>(() => poll.LinkGroup(Creator, Guid.NewGuid(), Now)).Code);
    }
}
