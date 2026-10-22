using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.UnitTests.Notifications;

public class NotificationEmailTests
{
    private static readonly NotificationSettings AllOn = UserSettings.Default.Notifications;

    [Fact]
    public void EveryKindWithAnEmail_IsWantedByDefaultAndCanBeComposed()
    {
        foreach (var kind in Enum.GetValues<NotificationKind>().Where(NotificationEmail.ExistsFor))
        {
            var message = NotificationEmail.Compose(kind, "Board games", "Max", new Uri("https://pyf.example/groups/1"), "When: Fri");

            Assert.True(NotificationEmail.IsWanted(AllOn, kind), $"{kind} is not wanted with every email turned on.");
            Assert.Contains("Board games", message.Subject, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void KindsWithoutAnEmail_AreNeverWanted()
    {
        foreach (var kind in Enum.GetValues<NotificationKind>().Where(k => !NotificationEmail.ExistsFor(k)))
        {
            Assert.False(NotificationEmail.IsWanted(AllOn, kind), $"{kind} has no email but is wanted.");
        }
    }

    [Fact]
    public void IsWanted_FollowsTheSettingOfThatKindOnly()
    {
        var noConfirmations = AllOn with { EventConfirmed = false };

        Assert.False(NotificationEmail.IsWanted(noConfirmations, NotificationKind.EventConfirmed));
        Assert.True(NotificationEmail.IsWanted(noConfirmations, NotificationKind.EventCancelled));
    }
}
