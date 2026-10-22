using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Domain;

public class VotingTests
{
    private static readonly Guid Organizer = Guid.NewGuid();
    private static readonly Guid Participant = Guid.NewGuid();
    private static readonly DateTime Now = At(1, 0);

    private static Group CreateGroup()
    {
        var group = Group.Create(Organizer, "Friday", null, Range(2, 9, 23), TimeSpan.FromHours(2), null, Now);
        group.AddMember(Participant);
        group.SetAvailability(Participant, [Range(2, 18, 21)], Now);
        return group;
    }

    [Fact]
    public void StartVoting_CreatesSortedOptionsWithFreeMembers()
    {
        var group = CreateGroup();

        group.StartVoting([Range(2, 18, 20), Range(2, 10, 12)], At(2, 8), Now);

        Assert.Equal(GroupStatus.Voting, group.Status);
        Assert.Equal(At(2, 8), group.VotingDeadline);
        Assert.Equal([Range(2, 10, 12), Range(2, 18, 20)], group.Options.Select(o => o.Time));
        Assert.Empty(group.Options.First().FreeUserIds);
        Assert.Equal([Participant], group.Options.Last().FreeUserIds);
    }

    [Theory]
    [InlineData(0, "voting.invalid_option_count")]
    [InlineData(7, "voting.invalid_option_count")]
    public void StartVoting_RejectsWrongNumberOfOptions(int count, string code)
    {
        var options = Enumerable.Range(9, count).Select(hour => Range(2, hour, hour + 1));

        var exception = Assert.Throws<BusinessRuleViolationException>(() =>
            CreateGroup().StartVoting(options, null, Now));
        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public void StartVoting_RejectsOptionOutsideWindowAndPastDeadline()
    {
        var outside = Assert.Throws<BusinessRuleViolationException>(() =>
            CreateGroup().StartVoting([Range(3, 10, 12)], null, Now));
        var deadline = Assert.Throws<BusinessRuleViolationException>(() =>
            CreateGroup().StartVoting([Range(2, 10, 12)], Now, Now));

        Assert.Equal("voting.invalid_option", outside.Code);
        Assert.Equal("voting.invalid_deadline", deadline.Code);
    }

    [Fact]
    public void Vote_StoresAndChangesAnswer()
    {
        var group = CreateGroup();
        group.StartVoting([Range(2, 18, 20)], null, Now);
        var option = group.Options.Single();

        group.Vote(Participant, option.Id, VoteAnswer.Maybe, Now);
        group.Vote(Participant, option.Id, VoteAnswer.Yes, Now);

        var vote = Assert.Single(option.Votes);
        Assert.Equal(VoteAnswer.Yes, vote.Answer);
    }

    [Fact]
    public void Vote_RejectsAfterDeadlineAndWhenNotVoting()
    {
        var group = CreateGroup();
        group.StartVoting([Range(2, 18, 20)], At(1, 12), Now);
        var optionId = group.Options.Single().Id;

        var late = Assert.Throws<InvalidStateException>(() => group.Vote(Participant, optionId, VoteAnswer.Yes, At(1, 13)));
        group.CancelVoting();
        var closed = Assert.Throws<InvalidStateException>(() => group.Vote(Participant, optionId, VoteAnswer.Yes, Now));

        Assert.Equal("voting.deadline_passed", late.Code);
        Assert.Equal("voting.closed", closed.Code);
    }

    [Fact]
    public void CancelVoting_ReturnsToPlanningWithoutOptions()
    {
        var group = CreateGroup();
        group.StartVoting([Range(2, 18, 20)], At(2, 8), Now);

        group.CancelVoting();

        Assert.Equal(GroupStatus.Planning, group.Status);
        Assert.Empty(group.Options);
        Assert.Null(group.VotingDeadline);
    }

    [Fact]
    public void ScheduleEvent_EndsVoting()
    {
        var group = CreateGroup();
        group.StartVoting([Range(2, 18, 20)], null, Now);

        group.ScheduleEvent("Friday", null, Range(2, 18, 20), null, null, Now);

        Assert.Equal(GroupStatus.Scheduled, group.Status);
        Assert.Empty(group.Options);
    }

    [Fact]
    public void JoiningIsAllowedDuringVoting()
    {
        var group = CreateGroup();
        group.StartVoting([Range(2, 18, 20)], null, Now);

        group.AddMember(Guid.NewGuid());

        Assert.Equal(3, group.Members.Count);
    }

    [Fact]
    public void CancelledEventGroupCanBeScheduledAgain()
    {
        var group = CreateGroup();
        var first = group.ScheduleEvent("Friday", null, Range(2, 18, 20), null, null, Now);
        group.CancelEvent(first);

        var second = group.ScheduleEvent("Friday", null, Range(2, 19, 21), null, null, Now);

        Assert.Equal(EventStatus.Cancelled, first.Status);
        Assert.Equal(EventStatus.Confirmed, second.Status);
    }

    [Fact]
    public void UpdateEvent_ChangesDetailsAndResyncsCalendars()
    {
        var group = CreateGroup();
        var @event = group.ScheduleEvent("Friday", null, Range(2, 18, 20), null, null, Now);
        var connectionId = Guid.NewGuid();
        var link = @event.AddCalendarLink(connectionId);
        @event.MarkSynced(connectionId, "google-id");

        @event.Update("Pizza", "Bring games", "Home", "https://meet.example.com");

        Assert.Equal("Pizza", @event.Title);
        Assert.Equal("Home", @event.Location);
        Assert.Equal(SyncStatus.Pending, link.SyncStatus);

        group.CancelEvent(@event);
        var exception = Assert.Throws<InvalidStateException>(() => @event.Update("x", null, null, null));
        Assert.Equal("event.cancelled", exception.Code);
    }
}
