using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 007: 女主 Motif 叠加单元测试。
/// 测试 motif overlay 与 BGM duck 纯逻辑（无 Godot 依赖）。
/// </summary>
public class MotifOverlayTest
{
    private readonly MotifOverlayEngine _engine = new();

    // === AC9: Motif 叠加与 duck ===

    [Fact]
    public void AC9_PlayMotif_StartsFadeInAndDucksBgm()
    {
        var result = _engine.PlayMotif("heroine_a", 1000f);

        Assert.Equal(MotifPlayResult.FadeInStarted, result);
        Assert.Equal("heroine_a", _engine.ActiveCharacterId);
        Assert.Equal("heroine_a_motif", _engine.ActiveTrackId);
        Assert.Equal(MotifOverlayPhase.FadingIn, _engine.Phase);

        var mid = _engine.Update(500f);

        Assert.Equal(0.65f, mid.BgmVolume, 2);
        Assert.Equal(0.5f, mid.MotifVolume, 2);
    }

    [Fact]
    public void AC9_PlayMotif_CompletesAtDuckRatio()
    {
        _engine.PlayMotif("heroine_a", 1000f);

        var final = _engine.Update(1000f);

        Assert.Equal(MotifOverlayPhase.Active, final.Phase);
        Assert.Equal(MotifOverlayEngine.DefaultDuckRatio, final.BgmVolume, 2);
        Assert.Equal(1f, final.MotifVolume, 2);
    }

    [Fact]
    public void AC9_StopMotif_FadesOutAndRestoresBgm()
    {
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        var result = _engine.StopMotif(1000f);
        Assert.Equal(MotifPlayResult.FadeOutStarted, result);

        var mid = _engine.Update(500f);
        Assert.Equal(0.65f, mid.BgmVolume, 2);
        Assert.Equal(0.5f, mid.MotifVolume, 2);

        var final = _engine.Update(500f);
        Assert.Equal(MotifOverlayPhase.Idle, final.Phase);
        Assert.Equal(1f, final.BgmVolume, 2);
        Assert.Equal(0f, final.MotifVolume, 2);
        Assert.Null(final.ActiveTrackId);
    }

    // === Motif 不进入 Override 栈 ===

    [Fact]
    public void Motif_DoesNotModifyBgmOverrideStack()
    {
        var bgm = new BgmCrossfadeEngine();
        bgm.PushBgm("exploration", AudioState.Exploration, 0f, 0f);
        bgm.Update(0f);

        int before = bgm.StackDepth;
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        Assert.Equal(before, bgm.StackDepth);
        Assert.Equal("exploration", bgm.CurrentTrackId);
    }

    // === E10: Motif 与场景 BGM 冲突时基于当前有效音量 duck ===

    [Fact]
    public void E10_DuckUsesCurrentEffectiveBgmVolume()
    {
        _engine.SetBgmBaseVolume(0.6f);

        _engine.PlayMotif("heroine_a", 1000f);
        var final = _engine.Update(1000f);

        Assert.Equal(0.18f, final.BgmVolume, 2);
        Assert.Equal(1f, final.MotifVolume, 2);
    }

    [Fact]
    public void E10_StopRestoresToPreviousEffectiveBgmVolume()
    {
        _engine.SetBgmBaseVolume(0.6f);
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        _engine.StopMotif(1000f);
        var final = _engine.Update(1000f);

        Assert.Equal(0.6f, final.BgmVolume, 2);
        Assert.Equal(MotifOverlayPhase.Idle, final.Phase);
    }

    [Fact]
    public void E10_StateAttenuationChangedWhileActive_DuckStacksOnNewBase()
    {
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        _engine.SetBgmBaseVolume(AudioStateAttenuation.GetAttenuation(AudioState.Dialogue, AudioTrack.Bgm));

        Assert.Equal(0.18f, _engine.BgmVolume, 2);
        Assert.Equal(1f, _engine.MotifVolume, 2);
    }

    // === 多个偶遇不会同时触发多个 motif ===

    [Fact]
    public void MultipleMotifs_SwitchesByFadingOutOldBeforeNewFadeIn()
    {
        _engine.PlayMotif("heroine_a", 500f);
        _engine.Update(500f);

        var result = _engine.PlayMotif("heroine_b", 500f);

        Assert.Equal(MotifPlayResult.SwitchStarted, result);
        Assert.Equal("heroine_a", _engine.ActiveCharacterId);
        Assert.Equal("heroine_b", _engine.PendingCharacterId);
        Assert.Equal(MotifOverlayPhase.SwitchingOut, _engine.Phase);

        var afterOldFade = _engine.Update(500f);

        Assert.Equal("heroine_b", afterOldFade.ActiveCharacterId);
        Assert.Null(afterOldFade.PendingCharacterId);
        Assert.Equal(MotifOverlayPhase.FadingIn, afterOldFade.Phase);
        Assert.Equal(0f, afterOldFade.MotifVolume, 2);

        var final = _engine.Update(500f);

        Assert.Equal("heroine_b", final.ActiveCharacterId);
        Assert.Equal("heroine_b_motif", final.ActiveTrackId);
        Assert.Equal(MotifOverlayPhase.Active, final.Phase);
        Assert.Equal(1f, final.MotifVolume, 2);
    }

    [Fact]
    public void SameMotifRequest_ContinuesCurrentMotif()
    {
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        var result = _engine.PlayMotif("heroine_a", 500f);

        Assert.Equal(MotifPlayResult.SameMotifContinue, result);
        Assert.Equal(MotifOverlayPhase.Active, _engine.Phase);
        Assert.Equal("heroine_a_motif", _engine.ActiveTrackId);
    }

    [Fact]
    public void ResolveMotifTrackId_UsesCharacterMotifConvention()
    {
        Assert.Equal("heroine_a_motif", MotifOverlayEngine.ResolveMotifTrackId("heroine_a"));
    }

    [Fact]
    public void Reset_ClearsMotifAndRestoresBgmBaseVolume()
    {
        _engine.SetBgmBaseVolume(0.6f);
        _engine.PlayMotif("heroine_a", 0f);
        _engine.Update(0f);

        _engine.Reset();

        Assert.Equal(MotifOverlayPhase.Idle, _engine.Phase);
        Assert.False(_engine.IsMotifActive);
        Assert.Null(_engine.ActiveTrackId);
        Assert.Null(_engine.PendingTrackId);
        Assert.Equal(0.6f, _engine.BgmVolume, 2);
        Assert.Equal(0f, _engine.MotifVolume, 2);
    }
}
