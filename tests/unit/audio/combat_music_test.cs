using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 003: 自适应战斗音乐（6 段水平分层）单元测试。
/// 测试 CombatMusicEngine 纯逻辑（无 Godot 依赖）。
/// </summary>
public class CombatMusicTest
{
    private readonly CombatMusicEngine _engine = new();

    private static CombatMusicConfig CreateTestConfig(int bpm = 120) => new()
    {
        Bpm = bpm,
        BarLengthMs = CombatMusicConfig.CalculateBarLengthMs(bpm),
        PrepTrack = "combat_prep",
        ClashTrack = "combat_clash",
        AdvantageTrack = "combat_advantage",
        DisadvantageTrack = "combat_disadvantage",
        DesperationTrack = "combat_desperation",
        FinisherTrack = "combat_finisher"
    };

    // === AC2: 段落切换在小节线对齐 ===

    [Fact]
    public void AC2_StartCombat_ActivatesEngine_ReturnsPrep()
    {
        var config = CreateTestConfig();
        var track = _engine.StartCombat(config);

        Assert.True(_engine.IsActive);
        Assert.Equal(CombatSegment.Prep, _engine.CurrentSegment);
        Assert.Equal("combat_prep", track);
    }

    [Fact]
    public void AC2_SegmentSwitch_WaitsForBarBoundary_WhenFarFromBar()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);

        var result = _engine.RequestSegmentSwitch(CombatSegment.Advantage);

        Assert.Equal(SegmentSwitchResult.Scheduled, result);
        Assert.Equal(CombatSegment.Prep, _engine.CurrentSegment);
        Assert.Equal(CombatSegment.Advantage, _engine.PendingSegment);
    }

    [Fact]
    public void AC2_SegmentSwitch_Immediate_WhenWithinBarTolerance()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(1950f);

        var result = _engine.RequestSegmentSwitch(CombatSegment.Clash);

        Assert.Equal(SegmentSwitchResult.Immediate, result);
        Assert.Equal(CombatSegment.Clash, _engine.CurrentSegment);
        Assert.Null(_engine.PendingSegment);
    }

    [Fact]
    public void AC2_PendingSwitch_CompletesAtBarBoundary()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);
        _engine.RequestSegmentSwitch(CombatSegment.Advantage);

        _engine.UpdatePlaybackPosition(1920f);
        var switched = _engine.TryCompletePendingSwitch();

        Assert.Equal(CombatSegment.Advantage, switched);
        Assert.Equal(CombatSegment.Advantage, _engine.CurrentSegment);
        Assert.Null(_engine.PendingSegment);
    }

    [Fact]
    public void AC2_PendingSwitch_DoesNotComplete_WhenFarFromBar()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);
        _engine.RequestSegmentSwitch(CombatSegment.Advantage);

        _engine.UpdatePlaybackPosition(1000f);
        var switched = _engine.TryCompletePendingSwitch();

        Assert.Null(switched);
        Assert.Equal(CombatSegment.Prep, _engine.CurrentSegment);
    }

    // === AC3: Finisher 立即切换 ===

    [Fact]
    public void AC3_Finisher_SwitchesImmediately_RegardlessOfBarPosition()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);

        var result = _engine.RequestSegmentSwitch(CombatSegment.Finisher);

        Assert.Equal(SegmentSwitchResult.Immediate, result);
        Assert.Equal(CombatSegment.Finisher, _engine.CurrentSegment);
    }

    [Fact]
    public void AC3_Finisher_OverridesPendingSwitch()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);
        _engine.RequestSegmentSwitch(CombatSegment.Advantage);

        var result = _engine.RequestSegmentSwitch(CombatSegment.Finisher);

        Assert.Equal(SegmentSwitchResult.Immediate, result);
        Assert.Equal(CombatSegment.Finisher, _engine.CurrentSegment);
        Assert.Null(_engine.PendingSegment);
    }

    // === F3 判定逻辑优先级 ===

    [Fact]
    public void F3_FinisherTriggered_ReturnsFinisher()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 1.0f, staminaRatio: 1.0f,
            advantageStreak: 0, finisherTriggered: true);

        Assert.Equal(CombatSegment.Finisher, segment);
    }

    [Fact]
    public void F3_LowHpAndStamina_ReturnsDesperation()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.10f, staminaRatio: 0.15f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.Equal(CombatSegment.Desperation, segment);
    }

    [Fact]
    public void F3_DesperationBoundary_Hp015Stamina020_NotDesperation()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.15f, staminaRatio: 0.20f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.NotEqual(CombatSegment.Desperation, segment);
    }

    [Fact]
    public void F3_LowHpOnly_ReturnsDisadvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.25f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.Equal(CombatSegment.Disadvantage, segment);
    }

    [Fact]
    public void F3_Hp030Exactly_NotDisadvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.30f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.NotEqual(CombatSegment.Disadvantage, segment);
    }

    [Fact]
    public void F3_CounterStreak2_ReturnsDisadvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.80f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false,
            counterStreak: 2);

        Assert.Equal(CombatSegment.Disadvantage, segment);
    }

    [Fact]
    public void F3_CounterStreak1_NotDisadvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.80f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false,
            counterStreak: 1);

        Assert.NotEqual(CombatSegment.Disadvantage, segment);
    }

    [Fact]
    public void F3_AdvantageStreak2_ReturnsAdvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.80f, staminaRatio: 0.80f,
            advantageStreak: 2, finisherTriggered: false);

        Assert.Equal(CombatSegment.Advantage, segment);
    }

    [Fact]
    public void F3_InActionWindow_ReturnsClash()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.80f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false,
            isInActionWindow: true);

        Assert.Equal(CombatSegment.Clash, segment);
    }

    [Fact]
    public void F3_Default_ReturnsPrep()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.80f, staminaRatio: 0.80f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.Equal(CombatSegment.Prep, segment);
    }

    [Fact]
    public void F3_Finisher_HigherPriority_ThanDesperation()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.05f, staminaRatio: 0.05f,
            advantageStreak: 5, finisherTriggered: true);

        Assert.Equal(CombatSegment.Finisher, segment);
    }

    [Fact]
    public void F3_Desperation_HigherPriority_ThanDisadvantage()
    {
        var segment = CombatMusicEngine.EvaluateCombatState(
            hpRatio: 0.10f, staminaRatio: 0.15f,
            advantageStreak: 0, finisherTriggered: false);

        Assert.Equal(CombatSegment.Desperation, segment);
    }

    // === AC4: 短 crossfade（fadeOut=500ms, fadeIn=300ms）===

    [Fact]
    public void AC4_CombatFadeValues_Correct()
    {
        Assert.Equal(500f, CombatMusicEngine.CombatFadeOutMs);
        Assert.Equal(300f, CombatMusicEngine.CombatFadeInMs);
    }

    // === AC5: Boss 战遵循相同 6 段结构 ===

    [Fact]
    public void AC5_BossConfig_UsesIndependentTracks_SameStructure()
    {
        var bossConfig = new CombatMusicConfig
        {
            Bpm = 140,
            BarLengthMs = CombatMusicConfig.CalculateBarLengthMs(140),
            PrepTrack = "boss_prep",
            ClashTrack = "boss_clash",
            AdvantageTrack = "boss_advantage",
            DisadvantageTrack = "boss_disadvantage",
            DesperationTrack = "boss_desperation",
            FinisherTrack = "boss_finisher"
        };

        var track = _engine.StartCombat(bossConfig);

        Assert.Equal("boss_prep", track);
        Assert.Equal("boss_finisher", bossConfig.GetTrack(CombatSegment.Finisher));
        Assert.Equal("boss_desperation", bossConfig.GetTrack(CombatSegment.Desperation));
    }

    // === AC6: 战斗结束 fade_out_ms=2000 ===

    [Fact]
    public void AC6_StopCombat_ReturnsBattleEndFadeOut()
    {
        var config = CreateTestConfig();
        _engine.StartCombat(config);

        float fadeOut = _engine.StopCombat();

        Assert.Equal(2000f, fadeOut);
        Assert.False(_engine.IsActive);
    }

    [Fact]
    public void AC6_StopCombat_ClearsPendingSegment()
    {
        var config = CreateTestConfig();
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(500f);
        _engine.RequestSegmentSwitch(CombatSegment.Advantage);

        _engine.StopCombat();

        Assert.Null(_engine.PendingSegment);
    }

    // === BarLengthMs 计算 ===

    [Fact]
    public void BarLengthMs_120BPM_4Beats_Is2000ms()
    {
        float barMs = CombatMusicConfig.CalculateBarLengthMs(120, 4);
        Assert.Equal(2000f, barMs);
    }

    [Fact]
    public void BarLengthMs_140BPM_4Beats()
    {
        float barMs = CombatMusicConfig.CalculateBarLengthMs(140, 4);
        float expected = 4f * 60_000f / 140f;
        Assert.Equal(expected, barMs, precision: 1);
    }

    [Fact]
    public void BarLengthMs_ZeroBPM_ReturnDefault()
    {
        float barMs = CombatMusicConfig.CalculateBarLengthMs(0);
        Assert.Equal(2000f, barMs);
    }

    // === GetMsUntilNextBar ===

    [Fact]
    public void GetMsUntilNextBar_AtMidBar_Returns1000ms()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(1000f);

        float remain = _engine.GetMsUntilNextBar();

        Assert.Equal(1000f, remain);
    }

    [Fact]
    public void GetMsUntilNextBar_AtBarStart_ReturnsFullBar()
    {
        var config = CreateTestConfig(120);
        _engine.StartCombat(config);
        _engine.UpdatePlaybackPosition(0f);

        float remain = _engine.GetMsUntilNextBar();

        Assert.Equal(2000f, remain);
    }

    // === AlreadyInSegment ===

    [Fact]
    public void RequestSwitch_SameSegment_ReturnsAlreadyInSegment()
    {
        var config = CreateTestConfig();
        _engine.StartCombat(config);

        var result = _engine.RequestSegmentSwitch(CombatSegment.Prep);

        Assert.Equal(SegmentSwitchResult.AlreadyInSegment, result);
    }

    // === NoConfig ===

    [Fact]
    public void RequestSwitch_WhenNotActive_ReturnsNoConfig()
    {
        var result = _engine.RequestSegmentSwitch(CombatSegment.Clash);

        Assert.Equal(SegmentSwitchResult.NoConfig, result);
    }

    // === GetSegmentTrack ===

    [Fact]
    public void GetSegmentTrack_ReturnsCorrectTrack()
    {
        var config = CreateTestConfig();
        _engine.StartCombat(config);

        Assert.Equal("combat_prep", _engine.GetSegmentTrack(CombatSegment.Prep));
        Assert.Equal("combat_clash", _engine.GetSegmentTrack(CombatSegment.Clash));
        Assert.Equal("combat_advantage", _engine.GetSegmentTrack(CombatSegment.Advantage));
        Assert.Equal("combat_disadvantage", _engine.GetSegmentTrack(CombatSegment.Disadvantage));
        Assert.Equal("combat_desperation", _engine.GetSegmentTrack(CombatSegment.Desperation));
        Assert.Equal("combat_finisher", _engine.GetSegmentTrack(CombatSegment.Finisher));
    }
}
