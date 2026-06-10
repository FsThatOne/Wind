using Xunit;
using FengZhi.Foundation.NpcState;
using NpcStateClass = FengZhi.Foundation.NpcState.NpcState;

namespace FengZhi.Tests.Foundation.NpcStates;

public class NpcStateModelTests
{
    // ─── AC1: 8 个状态维度枚举验证 ──────────────────────────

    [Fact]
    public void LifeStatus_HasExpectedValues()
    {
        Assert.Equal(5, Enum.GetValues<LifeStatus>().Length);
    }

    [Fact]
    public void PresenceStatus_HasExpectedValues()
    {
        Assert.Equal(6, Enum.GetValues<PresenceStatus>().Length);
    }

    [Fact]
    public void AttitudeLevel_RangeIsNeg4ToPos3()
    {
        Assert.Equal(-4, (int)AttitudeLevel.DrawnSword);
        Assert.Equal(3, (int)AttitudeLevel.LifeDeath);
        Assert.Equal(8, Enum.GetValues<AttitudeLevel>().Length);
    }

    // ─── AC2: NpcState 持有所有维度 + TemplateId ─────────────

    [Fact]
    public void NpcState_HoldsTemplateId()
    {
        var state = new NpcStateClass("bai_ling");
        Assert.Equal("bai_ling", state.TemplateId);
    }

    [Fact]
    public void NpcState_HoldsAllDimensions()
    {
        var state = new NpcStateClass("test_npc");
        state.SetLife(LifeStatus.Alive, "init");
        state.SetPresence(PresenceStatus.NearbyVisible, "spawn");
        state.SetLocation(LocationStatus.Town, "scene_load");
        state.SetInteraction(InteractionStatus.DialogueAvailable, "quest");
        state.SetJourney(JourneyStage.InProgress, "trigger");
        state.SetRelationship(RelationshipStage.Friend, "story");
        state.SetAttitude(AttitudeLevel.Trusted, "formula");

        Assert.Equal(LifeStatus.Alive, state.Life);
        Assert.Equal(PresenceStatus.NearbyVisible, state.Presence);
        Assert.Equal(LocationStatus.Town, state.Location);
        Assert.Equal(InteractionStatus.DialogueAvailable, state.Interaction);
        Assert.Equal(JourneyStage.InProgress, state.Journey);
        Assert.Equal(RelationshipStage.Friend, state.Relationship);
        Assert.Equal(AttitudeLevel.Trusted, state.Attitude);
    }

    // ─── AC3: CreateDefault 安全默认值 ──────────────────────

    [Fact]
    public void CreateDefault_ReturnsExpectedDefaults()
    {
        var state = NpcStateClass.CreateDefault("bai_ling");

        Assert.Equal("bai_ling", state.TemplateId);
        Assert.Equal(LifeStatus.Unknown, state.Life);
        Assert.Equal(PresenceStatus.Unreachable, state.Presence);
        Assert.Equal(LocationStatus.Unknown, state.Location);
        Assert.Equal(InteractionStatus.NotInteractable, state.Interaction);
        Assert.Equal(JourneyStage.NotStarted, state.Journey);
        Assert.Equal(RelationshipStage.Stranger, state.Relationship);
        Assert.Equal(AttitudeLevel.Stranger, state.Attitude);
    }

    // ─── AC4: 状态变更必须附带来源 ─────────────────────────

    [Fact]
    public void SetLife_UpdatesLastChangeSource()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetLife(LifeStatus.Alive, "chapter1_intro");

        Assert.Equal("chapter1_intro", state.LastChangeSource);
    }

    [Fact]
    public void SetAttitude_UpdatesLastChangeSource()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetAttitude(AttitudeLevel.Friendly, "dialogue_choice_a");

        Assert.Equal("dialogue_choice_a", state.LastChangeSource);
    }

    // ─── AC5: StateChangeRecord 变更历史 ────────────────────

    [Fact]
    public void History_RecordsFieldChanges()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetLife(LifeStatus.Alive, "spawn");
        state.SetPresence(PresenceStatus.NearbyVisible, "enter_scene");

        Assert.Equal(2, state.History.Count);
        Assert.Equal("Life", state.History[0].Field);
        Assert.Equal("Unknown", state.History[0].OldValue);
        Assert.Equal("Alive", state.History[0].NewValue);
        Assert.Equal("spawn", state.History[0].Source);
    }

    [Fact]
    public void History_TruncatesAt20()
    {
        var state = NpcStateClass.CreateDefault("test");

        for (int i = 0; i < 25; i++)
        {
            state.SetLife(i % 2 == 0 ? LifeStatus.Alive : LifeStatus.Injured, $"source_{i}");
        }

        Assert.Equal(NpcStateClass.MaxHistoryLength, state.History.Count);
    }

    // ─── Flags 自定义标记 ───────────────────────────────────

    [Fact]
    public void SetFlag_AddsToFlags()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetFlag("jianghu_quest_given", "true", "quest_system");

        Assert.Equal("true", state.Flags["jianghu_quest_given"]);
    }

    [Fact]
    public void RemoveFlag_RemovesFromFlags()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetFlag("temp_buff", "active", "combat");
        state.RemoveFlag("temp_buff", "combat_end");

        Assert.False(state.Flags.ContainsKey("temp_buff"));
    }

    [Fact]
    public void RemoveFlag_NonExistent_NoOp()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.RemoveFlag("nonexistent", "test");
        // 不抛异常，history 不增加
        Assert.Empty(state.History);
    }

    // ─── 辅助属性 ──────────────────────────────────────────

    [Fact]
    public void IsDead_ReturnsTrueWhenDead()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetLife(LifeStatus.Dead, "killed");
        Assert.True(state.IsDead);
    }

    [Fact]
    public void IsInteractable_NotInteractable_ReturnsFalse()
    {
        var state = NpcStateClass.CreateDefault("test");
        Assert.False(state.IsInteractable);
    }

    [Fact]
    public void IsInteractable_DialogueAvailable_ReturnsTrue()
    {
        var state = NpcStateClass.CreateDefault("test");
        state.SetInteraction(InteractionStatus.DialogueAvailable, "test");
        Assert.True(state.IsInteractable);
    }
}
