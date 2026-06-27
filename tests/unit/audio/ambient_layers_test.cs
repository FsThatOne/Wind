using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 004: 环境音三层系统单元测试。
/// 测试 AmbientLayerEngine 纯逻辑（无 Godot 依赖）。
/// </summary>
public class AmbientLayersTest
{
    private readonly AmbientLayerEngine _engine = new();

    // === AC8: 三层同时激活 ===

    [Fact]
    public void AC8_ThreeLayers_SimultaneousActivation()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.SetLayer(AmbientLayer.Weather, "light_rain");
        _engine.SetLayer(AmbientLayer.TimeOfDay, "cicada_noon");

        _engine.Update(2000f);

        Assert.Equal("river", _engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
        Assert.Equal("light_rain", _engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
        Assert.Equal("cicada_noon", _engine.GetLayer(AmbientLayer.TimeOfDay).CurrentTrackId);
    }

    [Fact]
    public void AC8_ThreeLayers_IndependentVolumes_DuringFade()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river", 1000f);
        _engine.SetLayer(AmbientLayer.Weather, "light_rain", 2000f);
        _engine.SetLayer(AmbientLayer.TimeOfDay, "cicada_noon", 500f);

        _engine.Update(500f);

        Assert.Equal(0.5f, _engine.GetLayer(AmbientLayer.Terrain).Volume, 0.01f);
        Assert.Equal(0.25f, _engine.GetLayer(AmbientLayer.Weather).Volume, 0.01f);
        Assert.Equal(1.0f, _engine.GetLayer(AmbientLayer.TimeOfDay).Volume, 0.01f);
    }

    // === 各层独立淡入淡出 (default 2000ms) ===

    [Fact]
    public void FadeIn_DefaultDuration_Is2000ms()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.Update(1000f);

        Assert.Equal(0.5f, _engine.GetLayer(AmbientLayer.Terrain).Volume, 0.01f);
        Assert.True(_engine.GetLayer(AmbientLayer.Terrain).IsFading);
    }

    [Fact]
    public void FadeIn_CompleteAfterDuration()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.Update(2000f);

        Assert.Equal(1.0f, _engine.GetLayer(AmbientLayer.Terrain).Volume, 0.01f);
        Assert.False(_engine.GetLayer(AmbientLayer.Terrain).IsFading);
        Assert.Equal("river", _engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
    }

    [Fact]
    public void FadeOut_WhenTrackSetToNull()
    {
        _engine.SetLayer(AmbientLayer.Weather, "rain");
        _engine.Update(2000f);
        Assert.Equal(1.0f, _engine.GetLayer(AmbientLayer.Weather).Volume);

        _engine.SetLayer(AmbientLayer.Weather, null);
        _engine.Update(1000f);

        Assert.Equal(0.5f, _engine.GetLayer(AmbientLayer.Weather).Volume, 0.01f);
    }

    [Fact]
    public void FadeOut_CompleteAfterDuration()
    {
        _engine.SetLayer(AmbientLayer.Weather, "rain");
        _engine.Update(2000f);

        _engine.SetLayer(AmbientLayer.Weather, null);
        _engine.Update(2000f);

        Assert.Equal(0f, _engine.GetLayer(AmbientLayer.Weather).Volume);
        Assert.Null(_engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
    }

    // === 场景切换：地形必换，天气/时辰若无变化则保持 ===

    [Fact]
    public void SceneChange_TerrainAlwaysSwitches()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.Update(2000f);

        var config = new SceneAudioConfig { TerrainTrack = "forest", WeatherTrack = null, TimeOfDayTrack = null };
        _engine.OnSceneAudioConfig(config);

        Assert.True(_engine.GetLayer(AmbientLayer.Terrain).IsFading);
        Assert.Equal("forest", _engine.GetLayer(AmbientLayer.Terrain).PendingTrackId);
    }

    [Fact]
    public void SceneChange_WeatherUnchanged_NoFade()
    {
        _engine.SetLayer(AmbientLayer.Weather, "rain");
        _engine.Update(2000f);

        var config = new SceneAudioConfig { TerrainTrack = "forest", WeatherTrack = "rain", TimeOfDayTrack = null };
        _engine.OnSceneAudioConfig(config);

        Assert.False(_engine.GetLayer(AmbientLayer.Weather).IsFading);
        Assert.Equal("rain", _engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
    }

    [Fact]
    public void SceneChange_TimeOfDayChanged_Fades()
    {
        _engine.SetLayer(AmbientLayer.TimeOfDay, "cicada_noon");
        _engine.Update(2000f);

        var config = new SceneAudioConfig { TerrainTrack = "river", WeatherTrack = null, TimeOfDayTrack = "owl_night" };
        _engine.OnSceneAudioConfig(config);

        Assert.True(_engine.GetLayer(AmbientLayer.TimeOfDay).IsFading);
    }

    // === E8: 层缺失 (null trackId) ===

    [Fact]
    public void E8_NullTrackId_LayerSilent_OthersNormal()
    {
        var config = new SceneAudioConfig { TerrainTrack = "river", WeatherTrack = null, TimeOfDayTrack = "cicada" };
        _engine.OnSceneAudioConfig(config);
        _engine.Update(2000f);

        Assert.Equal("river", _engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
        Assert.Null(_engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
        Assert.Equal("cicada", _engine.GetLayer(AmbientLayer.TimeOfDay).CurrentTrackId);
    }

    [Fact]
    public void E8_AllThreeNull_SystemDoesNotCrash()
    {
        var config = new SceneAudioConfig { TerrainTrack = null, WeatherTrack = null, TimeOfDayTrack = null };
        _engine.OnSceneAudioConfig(config);
        _engine.Update(2000f);

        Assert.Null(_engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
        Assert.Null(_engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
        Assert.Null(_engine.GetLayer(AmbientLayer.TimeOfDay).CurrentTrackId);
    }

    [Fact]
    public void E8_EmptyString_TreatedAsNull()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "");
        _engine.Update(2000f);

        Assert.Null(_engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
    }

    // === 同 track 重复调用不触发 fade ===

    [Fact]
    public void SameTrack_NoFadeTriggered()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.Update(2000f);

        _engine.SetLayer(AmbientLayer.Terrain, "river");
        Assert.False(_engine.GetLayer(AmbientLayer.Terrain).IsFading);
    }

    // === IsAnyFading ===

    [Fact]
    public void IsAnyFading_TrueWhileFading()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        Assert.True(_engine.IsAnyFading);
    }

    [Fact]
    public void IsAnyFading_FalseWhenAllComplete()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.Update(2000f);
        Assert.False(_engine.IsAnyFading);
    }

    // === StopAll ===

    [Fact]
    public void StopAll_ResetsAllLayers()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river");
        _engine.SetLayer(AmbientLayer.Weather, "rain");
        _engine.Update(2000f);

        _engine.StopAll();

        Assert.Null(_engine.GetLayer(AmbientLayer.Terrain).CurrentTrackId);
        Assert.Null(_engine.GetLayer(AmbientLayer.Weather).CurrentTrackId);
        Assert.Equal(0f, _engine.GetLayer(AmbientLayer.Terrain).Volume);
    }

    // === CustomFadeMs ===

    [Fact]
    public void CustomFadeMs_RespectsValue()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river", 500f);
        _engine.Update(500f);

        Assert.Equal(1.0f, _engine.GetLayer(AmbientLayer.Terrain).Volume, 0.01f);
        Assert.False(_engine.GetLayer(AmbientLayer.Terrain).IsFading);
    }

    // === DefaultFadeMs 常量 ===

    [Fact]
    public void DefaultFadeMs_Is2000()
    {
        Assert.Equal(2000f, AmbientLayerEngine.DefaultFadeMs);
    }

    // === Mid-fade track override ===

    [Fact]
    public void MidFadeIn_TrackOverride_SetsNeedsStreamSwap()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river", 2000f);
        _engine.Update(1000f); // 50% fade-in

        _engine.SetLayer(AmbientLayer.Terrain, "forest", 2000f);

        var state = _engine.GetLayer(AmbientLayer.Terrain);
        Assert.True(state.NeedsStreamSwap);
        Assert.Equal("forest", state.PendingTrackId);
    }

    [Fact]
    public void MidFadeIn_TrackOverride_CompletesToNewTrack()
    {
        _engine.SetLayer(AmbientLayer.Terrain, "river", 1000f);
        _engine.Update(500f);

        _engine.SetLayer(AmbientLayer.Terrain, "forest", 1000f);
        var state = _engine.GetLayer(AmbientLayer.Terrain);
        state.NeedsStreamSwap = false; // simulate Manager consuming the flag

        _engine.Update(1000f);

        Assert.Equal("forest", state.CurrentTrackId);
        Assert.Equal(1.0f, state.Volume, 0.01f);
    }
}
