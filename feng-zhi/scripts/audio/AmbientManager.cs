using Godot;
using FengZhi.Foundation.Audio;
using AudioBusLayout = FengZhi.Foundation.Audio.AudioBusLayout;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// 环境音三层管理器。持有 3 个 AudioStreamPlayer（Terrain/Weather/TimeOfDay），
/// 逐帧驱动 AmbientLayerEngine 实现独立淡入淡出。
/// 作为 AudioDirector 的子节点运行。
/// </summary>
public partial class AmbientManager : Node
{
    private AudioStreamPlayer[] _players = null!;
    private readonly AmbientLayerEngine _engine = new();

    public AmbientLayerEngine Engine => _engine;

    public override void _Ready()
    {
        _players = new[]
        {
            CreatePlayer(AudioBusLayout.AmbientTerrain),
            CreatePlayer(AudioBusLayout.AmbientWeather),
            CreatePlayer(AudioBusLayout.AmbientTimeOfDay)
        };
    }

    public override void _Process(double delta)
    {
        if (!_engine.IsAnyFading) return;

        float deltaMs = (float)(delta * 1000.0);
        _engine.Update(deltaMs);

        for (int i = 0; i < 3; i++)
        {
            var state = _engine.GetLayer((AmbientLayer)i);
            var player = _players[i];

            if (state.NeedsStreamSwap)
            {
                state.NeedsStreamSwap = false;
                if (state.PendingTrackId != null)
                    PreparePlayer((AmbientLayer)i, state.PendingTrackId);
            }

            player.VolumeDb = BgmCrossfadeEngine.LinearToDb(state.Volume);

            if (!state.IsFading && state.Volume <= 0f && player.Playing)
                player.Stop();
        }
    }

    /// <summary>
    /// 设置指定层 track。
    /// </summary>
    public void SetLayer(AmbientLayer layer, string? trackId, float fadeMs = AmbientLayerEngine.DefaultFadeMs)
    {
        var state = _engine.GetLayer(layer);
        bool wasEmpty = state.CurrentTrackId == null && !state.IsFading;

        _engine.SetLayer(layer, trackId, fadeMs);

        // 首次设置（无旧轨）时立即准备播放；否则等 NeedsStreamSwap
        if (wasEmpty && !string.IsNullOrEmpty(trackId))
            PreparePlayer(layer, trackId);
    }

    /// <summary>
    /// 场景切换时应用完整配置。
    /// </summary>
    public void OnSceneAudioConfig(SceneAudioConfig config, float fadeMs = AmbientLayerEngine.DefaultFadeMs)
    {
        var terrainState = _engine.GetLayer(AmbientLayer.Terrain);
        bool terrainWasEmpty = terrainState.CurrentTrackId == null && !terrainState.IsFading;

        var weatherState = _engine.GetLayer(AmbientLayer.Weather);
        bool weatherWasEmpty = weatherState.CurrentTrackId == null && !weatherState.IsFading;

        var todState = _engine.GetLayer(AmbientLayer.TimeOfDay);
        bool todWasEmpty = todState.CurrentTrackId == null && !todState.IsFading;

        _engine.OnSceneAudioConfig(config, fadeMs);

        // 仅首次设置时立即准备播放；已有旧轨的层等 NeedsStreamSwap
        if (terrainWasEmpty && !string.IsNullOrEmpty(config.TerrainTrack))
            PreparePlayer(AmbientLayer.Terrain, config.TerrainTrack);

        if (weatherWasEmpty && !string.IsNullOrEmpty(config.WeatherTrack))
            PreparePlayer(AmbientLayer.Weather, config.WeatherTrack);

        if (todWasEmpty && !string.IsNullOrEmpty(config.TimeOfDayTrack))
            PreparePlayer(AmbientLayer.TimeOfDay, config.TimeOfDayTrack);
    }

    private void PreparePlayer(AmbientLayer layer, string trackId)
    {
        var player = _players[(int)layer];
        var stream = GD.Load<AudioStream>($"res://feng-zhi/assets/audio/ambient/{trackId}.ogg");
        if (stream == null)
        {
            GD.PushWarning($"[AmbientManager] Track not found: {trackId}");
            return;
        }

        player.Stream = stream;
        player.VolumeDb = -80f;
        player.Play();
    }

    private AudioStreamPlayer CreatePlayer(string bus)
    {
        var player = new AudioStreamPlayer();
        player.Bus = bus;
        player.VolumeDb = -80f;
        AddChild(player);
        return player;
    }
}
