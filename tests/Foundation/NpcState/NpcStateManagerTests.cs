using Xunit;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Events;

namespace FengZhi.Tests.Foundation.NpcStates;

public class NpcStateManagerTests
{
    private readonly EventBus _eventBus;
    private readonly NpcStateManager _manager;

    public NpcStateManagerTests()
    {
        _eventBus = new EventBus();
        _manager = new NpcStateManager(_eventBus);
        _manager.RegisterNpc("bai_ling");
        _manager.RegisterNpc("lao_zhang");
    }

    // ─── AC1: GetState 返回完整状态或 null ──────────────────

    [Fact]
    public void GetState_Registered_ReturnsState()
    {
        var state = _manager.GetState("bai_ling");
        Assert.NotNull(state);
        Assert.Equal("bai_ling", state!.TemplateId);
    }

    [Fact]
    public void GetState_NotRegistered_ReturnsNull()
    {
        Assert.Null(_manager.GetState("nonexistent"));
    }

    // ─── AC2: UpdateState 后发布 NpcStateChangedEvent ────────

    [Fact]
    public void UpdateLife_PublishesEvent()
    {
        NpcStateChangedEvent? received = null;
        _eventBus.Subscribe<NpcStateChangedEvent>(e => received = e);

        _manager.UpdateLife("bai_ling", LifeStatus.Alive, "chapter1");

        Assert.NotNull(received);
        Assert.Equal("bai_ling", received!.NpcId);
        Assert.Equal("Life", received.Field);
        Assert.Equal("Unknown", received.OldValue);
        Assert.Equal("Alive", received.NewValue);
        Assert.Equal("chapter1", received.Source);
    }

    [Fact]
    public void UpdateAttitude_PublishesEvent()
    {
        NpcStateChangedEvent? received = null;
        _eventBus.Subscribe<NpcStateChangedEvent>(e => received = e);

        _manager.UpdateAttitude("bai_ling", AttitudeLevel.Friendly, "dialogue");

        Assert.NotNull(received);
        Assert.Equal("Attitude", received!.Field);
        Assert.Equal("Friendly", received.NewValue);
    }

    // ─── AC3: 对话锁定时排队，解锁后批量生效 ───────────────

    [Fact]
    public void DialogueLocked_ChangesQueued_NotAppliedImmediately()
    {
        _manager.SetDialogueLocked("bai_ling", true);

        var events = new List<NpcStateChangedEvent>();
        _eventBus.Subscribe<NpcStateChangedEvent>(e => events.Add(e));

        _manager.UpdatePresence("bai_ling", PresenceStatus.NearbyVisible, "quest");

        // 事件未触发
        Assert.Empty(events);
        // 状态未变（仍排队中）
        var state = _manager.GetState("bai_ling");
        Assert.Equal(PresenceStatus.Unreachable, state!.Presence);
    }

    [Fact]
    public void DialogueUnlocked_PendingChangesApplied()
    {
        _manager.SetDialogueLocked("bai_ling", true);
        _manager.UpdatePresence("bai_ling", PresenceStatus.NearbyVisible, "quest");
        _manager.UpdateLocation("bai_ling", LocationStatus.Town, "scene");

        var events = new List<NpcStateChangedEvent>();
        _eventBus.Subscribe<NpcStateChangedEvent>(e => events.Add(e));

        _manager.SetDialogueLocked("bai_ling", false);

        Assert.Equal(2, events.Count);
        var state = _manager.GetState("bai_ling");
        Assert.Equal(PresenceStatus.NearbyVisible, state!.Presence);
        Assert.Equal(LocationStatus.Town, state.Location);
    }

    // ─── AC4: GetByPresence ─────────────────────────────────

    [Fact]
    public void GetByPresence_FiltersCorrectly()
    {
        _manager.UpdatePresence("bai_ling", PresenceStatus.NearbyVisible, "test");
        _manager.UpdatePresence("lao_zhang", PresenceStatus.FarRegion, "test");

        var nearby = _manager.GetByPresence(PresenceStatus.NearbyVisible);
        Assert.Single(nearby);
        Assert.Equal("bai_ling", nearby[0].TemplateId);
    }

    // ─── AC5: GetByAttitude ─────────────────────────────────

    [Fact]
    public void GetByAttitude_FiltersCorrectly()
    {
        _manager.UpdateAttitude("bai_ling", AttitudeLevel.Trusted, "story");

        var trusted = _manager.GetByAttitude(AttitudeLevel.Trusted);
        Assert.Single(trusted);
        Assert.Equal("bai_ling", trusted[0].TemplateId);
    }

    // ─── AC6: 死亡 NPC 非 Life 字段不可修改 ────────────────

    [Fact]
    public void DeadNpc_NonLifeFieldRejected()
    {
        _manager.UpdateLife("bai_ling", LifeStatus.Dead, "combat");

        bool result = _manager.UpdatePresence("bai_ling", PresenceStatus.NearbyVisible, "test");
        Assert.False(result);

        var state = _manager.GetState("bai_ling");
        Assert.Equal(PresenceStatus.Unreachable, state!.Presence);
    }

    [Fact]
    public void DeadNpc_LifeFieldStillModifiable()
    {
        _manager.UpdateLife("bai_ling", LifeStatus.Dead, "combat");

        // 复活场景：可以修改 Life
        bool result = _manager.UpdateLife("bai_ling", LifeStatus.Alive, "resurrection");
        Assert.True(result);

        var state = _manager.GetState("bai_ling");
        Assert.Equal(LifeStatus.Alive, state!.Life);
    }

    // ─── 边界：未注册 NPC 操作返回 false ────────────────────

    [Fact]
    public void UpdateLife_UnregisteredNpc_ReturnsFalse()
    {
        Assert.False(_manager.UpdateLife("ghost", LifeStatus.Alive, "test"));
    }

    // ─── 边界：多次注册同一 NPC 幂等 ───────────────────────

    [Fact]
    public void RegisterNpc_Twice_NoError()
    {
        _manager.RegisterNpc("bai_ling"); // 重复注册
        var state = _manager.GetState("bai_ling");
        Assert.NotNull(state);
    }
}
