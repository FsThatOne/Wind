using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 006: 演出音频接管与跳过恢复 — 集成测试。
/// 测试 CutsceneAudioEngine + SfxPoolEngine 协作逻辑。
/// </summary>
public class CutsceneAudioTest
{
    private readonly CutsceneAudioEngine _cutsceneEngine = new();
    private readonly SfxPoolEngine _sfxPool = new();

    // === AC4: 演出跳过时所有演出 SFX 200ms 淡出 ===

    [Fact]
    public void AC4_Skip_ReturnsEndResult_EngineDeactivates()
    {
        _cutsceneEngine.Begin("cutscene_bgm_01");
        Assert.True(_cutsceneEngine.IsActive);

        var result = _cutsceneEngine.Skip();
        Assert.False(_cutsceneEngine.IsActive);
        Assert.False(result.ShouldRestoreCombatPosition);
    }

    [Fact]
    public void AC4_Skip_CutsceneSfxSlots_CanBeQueried()
    {
        float t = 1000f;

        _sfxPool.TryPlay("cs_impact", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);
        _sfxPool.TryPlay("cs_whoosh", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t + 1, bypassCooldown: true);
        _sfxPool.TryPlay("env_wind", SfxPriority.P3, "environment", t + 2);

        var cutsceneSlots = _sfxPool.GetActiveSlotsBySource(CutsceneAudioEngine.CutsceneSourceId);
        Assert.Equal(2, cutsceneSlots.Count);
    }

    // === PLAY_BGM 压入 Override 栈 ===

    [Fact]
    public void PlayBgm_EngineTracksCutsceneBgm()
    {
        _cutsceneEngine.Begin("cutscene_dramatic");
        Assert.Equal("cutscene_dramatic", _cutsceneEngine.CutsceneBgmTrackId);
        Assert.True(_cutsceneEngine.IsActive);
    }

    // === PLAY_SFX 使用 P0 + bypass cooldown ===

    [Fact]
    public void PlaySfx_P0_BypassCooldown_SameSfxPlaysRepeatedly()
    {
        float t = 1000f;

        var r1 = _sfxPool.TryPlay("cs_impact", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);
        Assert.Equal(PlaySfxResult.Played, r1.Result);

        // 10ms 后同一 SFX 再次请求 — bypass cooldown 应允许
        var r2 = _sfxPool.TryPlay("cs_impact", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t + 10, bypassCooldown: true);
        Assert.Equal(PlaySfxResult.Played, r2.Result);
    }

    [Fact]
    public void PlaySfx_P0_WithoutBypass_Blocked()
    {
        float t = 1000f;

        _sfxPool.TryPlay("cs_impact", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t);

        // 10ms 后不 bypass — 应被 cooldown 阻止
        var r2 = _sfxPool.TryPlay("cs_impact", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t + 10);
        Assert.Equal(PlaySfxResult.CooldownBlocked, r2.Result);
    }

    // === E3: 战斗中触发演出，保存/恢复位置 ===

    [Fact]
    public void E3_CombatPosition_SavedOnBegin_RestoredOnEnd()
    {
        float combatPos = 42500f; // 42.5s 位置

        _cutsceneEngine.Begin("cutscene_e3", combatPositionMs: combatPos);
        Assert.True(_cutsceneEngine.HasSavedCombatPosition);
        Assert.Equal(combatPos, _cutsceneEngine.SavedCombatPositionMs);

        var result = _cutsceneEngine.End();
        Assert.True(result.ShouldRestoreCombatPosition);
        Assert.Equal(combatPos, result.CombatPositionMs);

        // End 后状态清除
        Assert.False(_cutsceneEngine.HasSavedCombatPosition);
    }

    [Fact]
    public void E3_CombatPosition_SavedOnBegin_RestoredOnSkip()
    {
        float combatPos = 15000f;

        _cutsceneEngine.Begin("cutscene_battle_drama", combatPositionMs: combatPos);
        var result = _cutsceneEngine.Skip();

        Assert.True(result.ShouldRestoreCombatPosition);
        Assert.Equal(combatPos, result.CombatPositionMs);
    }

    [Fact]
    public void E3_NoCombat_NoPositionSaved()
    {
        _cutsceneEngine.Begin("cutscene_exploration");
        var result = _cutsceneEngine.End();

        Assert.False(result.ShouldRestoreCombatPosition);
        Assert.Equal(0f, result.CombatPositionMs);
    }

    // === PARALLEL 步骤多 SFX 不受 cooldown 限制 ===

    [Fact]
    public void Parallel_MultipleSfx_AllPlay_BypassCooldown()
    {
        float t = 1000f;

        // 模拟 PARALLEL 步骤中多个 SFX 同时触发
        var r1 = _sfxPool.TryPlay("slash_fx", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);
        var r2 = _sfxPool.TryPlay("slash_fx", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);
        var r3 = _sfxPool.TryPlay("explosion", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);

        Assert.Equal(PlaySfxResult.Played, r1.Result);
        // r2 同 sfxId 同时间 bypass cooldown — 播放
        Assert.Equal(PlaySfxResult.Played, r2.Result);
        Assert.Equal(PlaySfxResult.Played, r3.Result);
    }

    // === SkipFadeOutMs 常量 ===

    [Fact]
    public void Constants_SkipFadeOutMs_200()
    {
        Assert.Equal(200f, CutsceneAudioEngine.SkipFadeOutMs);
    }

    // === Reset ===

    [Fact]
    public void Reset_ClearsAllState()
    {
        _cutsceneEngine.Begin("bgm", combatPositionMs: 5000f);
        _cutsceneEngine.Reset();

        Assert.False(_cutsceneEngine.IsActive);
        Assert.False(_cutsceneEngine.HasSavedCombatPosition);
        Assert.Null(_cutsceneEngine.CutsceneBgmTrackId);
    }

    // === 完整流程：Begin → PlaySfx → Skip → 验证 slot 可回收 ===

    [Fact]
    public void FullFlow_Begin_PlaySfx_Skip_SlotsReclaimable()
    {
        float t = 1000f;

        _cutsceneEngine.Begin("cutscene_01");

        // 播放 3 个演出 SFX
        _sfxPool.TryPlay("cs_fx1", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t, bypassCooldown: true);
        _sfxPool.TryPlay("cs_fx2", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t + 1, bypassCooldown: true);
        _sfxPool.TryPlay("cs_fx3", SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, t + 2, bypassCooldown: true);

        Assert.Equal(3, _sfxPool.ActiveCount);

        // 跳过 — Engine 标记结束
        _cutsceneEngine.Skip();
        Assert.False(_cutsceneEngine.IsActive);

        // Presentation 层会 fade out 并 MarkSlotFinished，模拟回收
        var slots = _sfxPool.GetActiveSlotsBySource(CutsceneAudioEngine.CutsceneSourceId);
        foreach (int i in slots)
            _sfxPool.MarkSlotFinished(i);

        Assert.Equal(0, _sfxPool.ActiveCount);
    }
}
