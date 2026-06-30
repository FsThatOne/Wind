using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class TransparencySignalRouterTest
{
    private readonly MockDialogueChannel _dialogue = new();
    private readonly MockPanelChannel _panel = new();
    private readonly MockAmbienceChannel _ambience = new();
    private readonly TransparencySignalRouter _router;

    public TransparencySignalRouterTest()
    {
        _router = new TransparencySignalRouter(_dialogue, _panel, _ambience);
    }

    [Fact]
    public void EmitHint_RoutesToDialogueChannel()
    {
        _router.EmitHint("npc1", "inst_a");

        Assert.Single(_dialogue.Notifications);
        Assert.Equal(("npc1", "inst_a"), _dialogue.Notifications[0]);
    }

    [Fact]
    public void EmitPerceived_RoutesToPanelChannel()
    {
        _router.EmitPerceived("npc1", "inst_a");

        Assert.Single(_panel.PerceivedNotifications);
        Assert.Equal(("npc1", "inst_a"), _panel.PerceivedNotifications[0]);
    }

    [Fact]
    public void EmitUrgent_RoutesToPanelAndAmbienceChannels()
    {
        _router.EmitUrgent("npc1", "inst_a");

        Assert.Single(_panel.UrgentNotifications);
        Assert.Single(_ambience.UrgentNotifications);
        Assert.Equal(("npc1", "inst_a"), _panel.UrgentNotifications[0]);
        Assert.Equal(("npc1", "inst_a"), _ambience.UrgentNotifications[0]);
    }

    [Fact]
    public void MultipleEmits_AllRouted()
    {
        _router.EmitHint("npc1", "a");
        _router.EmitPerceived("npc1", "a");
        _router.EmitUrgent("npc1", "a");

        Assert.Single(_dialogue.Notifications);
        Assert.Single(_panel.PerceivedNotifications);
        Assert.Single(_panel.UrgentNotifications);
        Assert.Single(_ambience.UrgentNotifications);
    }

    private sealed class MockDialogueChannel : IDialogueSignalChannel
    {
        public List<(string npcId, string instId)> Notifications { get; } = new();
        public void OnHinted(string npcId, string instanceId) => Notifications.Add((npcId, instanceId));
    }

    private sealed class MockPanelChannel : IPanelSignalChannel
    {
        public List<(string npcId, string instId)> PerceivedNotifications { get; } = new();
        public List<(string npcId, string instId)> UrgentNotifications { get; } = new();
        public void OnPerceived(string npcId, string instanceId) => PerceivedNotifications.Add((npcId, instanceId));
        public void OnUrgent(string npcId, string instanceId) => UrgentNotifications.Add((npcId, instanceId));
    }

    private sealed class MockAmbienceChannel : IAmbienceSignalChannel
    {
        public List<(string npcId, string instId)> UrgentNotifications { get; } = new();
        public void OnUrgent(string npcId, string instanceId) => UrgentNotifications.Add((npcId, instanceId));
    }
}
