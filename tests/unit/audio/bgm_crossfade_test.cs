using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 002: BGM 管理 + 等功率 Crossfade 单元测试。
/// 测试 BgmCrossfadeEngine 纯逻辑（无 Godot 依赖）。
/// </summary>
public class BgmCrossfadeTest
{
    private readonly BgmCrossfadeEngine _engine = new();

    // === AC1: 场景 BGM crossfade（1500ms fade_out + 800ms fade_in）===

    [Fact]
    public void AC1_PlayBgm_StartsCrossfade()
    {
        var result = _engine.PlayBgm("bgm_jiangnan", 1500f, 800f);

        Assert.Equal(PlayResult.Crossfading, result);
        Assert.True(_engine.IsCrossfading);
        Assert.Equal("bgm_jiangnan", _engine.PendingTrackId);
    }

    [Fact]
    public void AC1_FadeOutCurve_CosEqualPower_MidpointIsMinus3dB()
    {
        float mid = BgmCrossfadeEngine.EqualPowerFadeOut(0.5f);
        float expectedDb = BgmCrossfadeEngine.LinearToDb(mid);

        Assert.InRange(expectedDb, -3.1f, -2.9f);
    }

    [Fact]
    public void AC1_FadeInCurve_SinEqualPower_MidpointIsMinus3dB()
    {
        float mid = BgmCrossfadeEngine.EqualPowerFadeIn(0.5f);
        float expectedDb = BgmCrossfadeEngine.LinearToDb(mid);

        Assert.InRange(expectedDb, -3.1f, -2.9f);
    }

    [Fact]
    public void AC1_CrossfadeCompletes_AfterFullDuration()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);

        SimulateMs(1500f);

        Assert.Equal("bgm_a", _engine.CurrentTrackId);
        Assert.False(_engine.IsCrossfading);
    }

    [Fact]
    public void AC1_FadeOutVolume_DecreasesOverTime()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);

        var v1 = _engine.Update(100f);
        var v2 = _engine.Update(400f);
        var v3 = _engine.Update(500f);

        Assert.True(v1.FadeOutVolume > v2.FadeOutVolume);
        Assert.True(v2.FadeOutVolume > v3.FadeOutVolume);
    }

    [Fact]
    public void AC1_FadeInVolume_IncreasesOverTime()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);

        var v1 = _engine.Update(100f);
        var v2 = _engine.Update(200f);
        var v3 = _engine.Update(300f);

        Assert.True(v1.FadeInVolume < v2.FadeInVolume);
        Assert.True(v2.FadeInVolume < v3.FadeInVolume);
    }

    [Fact]
    public void AC1_FadeOutEndsAt1500ms_FadeInEndsAt800ms()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);

        // 使用单次大步进避免浮点累积误差
        var atFadeIn = _engine.Update(800f);
        Assert.True(atFadeIn.FadeInComplete);
        Assert.False(atFadeIn.FadeOutComplete);

        var atFadeOut = _engine.Update(700f);
        Assert.True(atFadeOut.FadeOutComplete);
        Assert.True(atFadeOut.NewTrackReady);
    }

    // === AC2: Override 栈 push/pop 正确管理 ===

    [Fact]
    public void AC2_PushBgm_AddsToStack()
    {
        _engine.PlayBgm("exploration_bgm", 0f, 0f);
        SimulateMs(1f);

        _engine.PushBgm("combat_bgm", AudioState.Combat, 1500f, 800f);
        Assert.Equal(1, _engine.StackDepth);
        Assert.Equal("combat_bgm", _engine.PendingTrackId);
    }

    [Fact]
    public void AC2_PushThreeLayers_StackCorrect()
    {
        _engine.PushBgm("exploration", AudioState.Exploration, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("combat", AudioState.Combat, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("cutscene", AudioState.Cutscene, 0f, 0f);

        Assert.Equal(3, _engine.StackDepth);
    }

    [Fact]
    public void AC2_PopBgm_RestoresPrevious()
    {
        _engine.PushBgm("exploration", AudioState.Exploration, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("combat", AudioState.Combat, 0f, 0f);
        SimulateMs(1f);

        var result = _engine.PopBgm(1500f, 800f);
        Assert.Equal(PlayResult.Crossfading, result);
        Assert.Equal("exploration", _engine.PendingTrackId);
        Assert.Equal(1, _engine.StackDepth);
    }

    [Fact]
    public void AC2_PopEmptyStack_ReturnsIgnored()
    {
        var result = _engine.PopBgm();
        Assert.Equal(PlayResult.Ignored, result);
    }

    // === AC3: 栈溢出替换栈顶（Edge Case E1）===

    [Fact]
    public void AC3_StackOverflow_ReplacesTop()
    {
        _engine.PushBgm("exploration", AudioState.Exploration, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("combat", AudioState.Combat, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("cutscene", AudioState.Cutscene, 0f, 0f);
        SimulateMs(1f);

        Assert.Equal(3, _engine.StackDepth);

        _engine.PushBgm("new_cutscene", AudioState.Cutscene, 1000f, 500f);

        Assert.Equal(3, _engine.StackDepth);
        Assert.Equal("new_cutscene", _engine.PendingTrackId);
    }

    [Fact]
    public void AC3_StackOverflow_PopReturnsSecondLayer()
    {
        _engine.PushBgm("exploration", AudioState.Exploration, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("combat", AudioState.Combat, 0f, 0f);
        SimulateMs(1f);
        _engine.PushBgm("cutscene", AudioState.Cutscene, 0f, 0f);
        SimulateMs(1f);

        _engine.PushBgm("new_cutscene", AudioState.Cutscene, 0f, 0f);
        SimulateMs(1f);

        _engine.PopBgm(0f, 0f);
        Assert.Equal("combat", _engine.PendingTrackId);
    }

    // === AC4: 同曲续播跳过 crossfade（Edge Case E7）===

    [Fact]
    public void AC4_SameTrack_PlayBgm_SkipsCrossfade()
    {
        _engine.PlayBgm("jiangnan_theme", 0f, 0f);
        SimulateMs(1f);

        var result = _engine.PlayBgm("jiangnan_theme", 1500f, 800f);

        Assert.Equal(PlayResult.SameTrackContinue, result);
        Assert.False(_engine.IsCrossfading);
    }

    [Fact]
    public void AC4_SameTrack_PushBgm_SkipsCrossfade()
    {
        _engine.PushBgm("jiangnan_theme", AudioState.Exploration, 0f, 0f);
        SimulateMs(1f);

        var result = _engine.PushBgm("jiangnan_theme", AudioState.Combat, 1500f, 800f);

        Assert.Equal(PlayResult.SameTrackContinue, result);
        Assert.False(_engine.IsCrossfading);
        Assert.Equal(2, _engine.StackDepth);
    }

    [Fact]
    public void AC4_DifferentTrack_DoesCrossfade()
    {
        _engine.PlayBgm("jiangnan_theme", 0f, 0f);
        SimulateMs(1f);

        var result = _engine.PlayBgm("saibei_theme", 1500f, 800f);

        Assert.Equal(PlayResult.Crossfading, result);
        Assert.True(_engine.IsCrossfading);
    }

    // === AC5: 极短场景过渡 — crossfade 未完成时新切换中断重开（Edge Case E2）===

    [Fact]
    public void AC5_InterruptCrossfade_StartsNewOne()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);
        SimulateMs(300f);
        Assert.True(_engine.IsCrossfading);

        var result = _engine.PlayBgm("bgm_b", 1500f, 800f);

        Assert.Equal(PlayResult.Crossfading, result);
        Assert.Equal("bgm_b", _engine.PendingTrackId);
        Assert.True(_engine.IsCrossfading);
    }

    [Fact]
    public void AC5_InterruptedCrossfade_ResetsProgress()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);
        SimulateMs(750f);

        _engine.PlayBgm("bgm_b", 1500f, 800f);

        var v = _engine.Update(0f);
        Assert.Equal(1f, v.FadeOutVolume, 2);
        Assert.Equal(0f, v.FadeInVolume, 2);
    }

    [Fact]
    public void AC5_InterruptedCrossfade_CompletesNewFade()
    {
        _engine.PlayBgm("bgm_a", 1500f, 800f);
        SimulateMs(300f);

        _engine.PlayBgm("bgm_b", 1000f, 500f);
        SimulateMs(1000f);

        Assert.Equal("bgm_b", _engine.CurrentTrackId);
        Assert.False(_engine.IsCrossfading);
    }

    // === AC6: SILENCE bgm_id — 淡出当前 BGM 但不播放新曲 ===

    [Fact]
    public void AC6_Silence_FadesOutOnly()
    {
        _engine.PlayBgm("bgm_a", 0f, 0f);
        SimulateMs(1f);

        var result = _engine.PlayBgm(BgmCrossfadeEngine.SilenceTrackId, 2000f, 0f);

        Assert.Equal(PlayResult.FadeToSilence, result);
        Assert.True(_engine.IsCrossfading);
    }

    [Fact]
    public void AC6_Silence_FadeInVolumeAlwaysZero()
    {
        _engine.PlayBgm("bgm_a", 0f, 0f);
        SimulateMs(1f);

        _engine.PlayBgm(BgmCrossfadeEngine.SilenceTrackId, 2000f, 800f);

        var v1 = _engine.Update(500f);
        var v2 = _engine.Update(500f);
        var v3 = _engine.Update(500f);

        Assert.Equal(0f, v1.FadeInVolume);
        Assert.Equal(0f, v2.FadeInVolume);
        Assert.Equal(0f, v3.FadeInVolume);
    }

    [Fact]
    public void AC6_Silence_CompletesAndSetsCurrentTrack()
    {
        _engine.PlayBgm("bgm_a", 0f, 0f);
        SimulateMs(1f);

        _engine.PlayBgm(BgmCrossfadeEngine.SilenceTrackId, 1000f, 0f);
        SimulateMs(1000f);

        Assert.Equal(BgmCrossfadeEngine.SilenceTrackId, _engine.CurrentTrackId);
        Assert.False(_engine.IsCrossfading);
    }

    // === 辅助: 等功率曲线数学验证 ===

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(1f, 0f)]
    public void EqualPowerFadeOut_BoundaryValues(float t, float expected)
    {
        float actual = BgmCrossfadeEngine.EqualPowerFadeOut(t);
        Assert.Equal(expected, actual, 3);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 1f)]
    public void EqualPowerFadeIn_BoundaryValues(float t, float expected)
    {
        float actual = BgmCrossfadeEngine.EqualPowerFadeIn(t);
        Assert.Equal(expected, actual, 3);
    }

    [Fact]
    public void EqualPower_SumOfSquares_IsConstant()
    {
        for (float t = 0f; t <= 1f; t += 0.1f)
        {
            float fadeOut = BgmCrossfadeEngine.EqualPowerFadeOut(t);
            float fadeIn = BgmCrossfadeEngine.EqualPowerFadeIn(t);
            float sumOfSquares = fadeOut * fadeOut + fadeIn * fadeIn;
            Assert.InRange(sumOfSquares, 0.99f, 1.01f);
        }
    }

    [Fact]
    public void LinearToDb_ZeroReturnsMinus80()
    {
        Assert.Equal(-80f, BgmCrossfadeEngine.LinearToDb(0f));
    }

    [Fact]
    public void LinearToDb_OneReturnsZero()
    {
        Assert.Equal(0f, BgmCrossfadeEngine.LinearToDb(1f), 2);
    }

    // === 辅助方法 ===

    private void SimulateMs(float totalMs, float stepMs = 16.67f)
    {
        float elapsed = 0f;
        while (elapsed < totalMs)
        {
            float step = MathF.Min(stepMs, totalMs - elapsed);
            _engine.Update(step);
            elapsed += step;
        }
    }
}
