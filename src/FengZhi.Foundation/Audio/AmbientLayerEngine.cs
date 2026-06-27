namespace FengZhi.Foundation.Audio;

/// <summary>
/// 环境音三层标识。
/// </summary>
public enum AmbientLayer
{
    Terrain,
    Weather,
    TimeOfDay
}

/// <summary>
/// 单层环境音 fade 状态。
/// </summary>
public sealed class AmbientLayerState
{
    public string? CurrentTrackId { get; private set; }
    public string? PendingTrackId { get; private set; }
    public float FadeElapsedMs { get; private set; }
    public float FadeDurationMs { get; private set; }
    public bool IsFading { get; private set; }
    public bool IsFadingOut { get; private set; }

    /// <summary>
    /// 当前线性音量 (0..1)。
    /// </summary>
    public float Volume { get; private set; } = 1f;

    public void SetTrack(string? trackId, float fadeMs)
    {
        if (string.IsNullOrEmpty(trackId)) trackId = null;

        if (trackId == CurrentTrackId && !IsFading)
            return;

        if (trackId == null)
        {
            if (CurrentTrackId == null && !IsFading)
                return;
            IsFadingOut = true;
            PendingTrackId = null;
        }
        else
        {
            if (CurrentTrackId != null && Volume > 0f)
            {
                IsFadingOut = true;
                PendingTrackId = trackId;
            }
            else
            {
                if (IsFading && !IsFadingOut && PendingTrackId != null && PendingTrackId != trackId)
                    NeedsStreamSwap = true;
                IsFadingOut = false;
                PendingTrackId = trackId;
            }
        }

        FadeElapsedMs = 0f;
        FadeDurationMs = fadeMs > 0f ? fadeMs : 1f;
        IsFading = true;
    }

    /// <summary>
    /// 逐帧更新 fade 进度。返回 true 表示整个过渡刚完成。
    /// </summary>
    public bool Update(float deltaMs)
    {
        if (!IsFading) return false;

        FadeElapsedMs += deltaMs;
        float t = FadeElapsedMs / FadeDurationMs;

        if (t >= 1f)
        {
            t = 1f;

            if (IsFadingOut && PendingTrackId != null)
            {
                // 两阶段过渡第一阶段结束：旧轨淡出完成，进入新轨淡入
                CurrentTrackId = null;
                IsFadingOut = false;
                FadeElapsedMs = 0f;
                Volume = 0f;
                NeedsStreamSwap = true;
                return false;
            }

            IsFading = false;
            CurrentTrackId = PendingTrackId;
            PendingTrackId = null;
            Volume = IsFadingOut ? 0f : 1f;
            IsFadingOut = false;
            return true;
        }

        Volume = IsFadingOut ? 1f - t : t;
        return false;
    }

    /// <summary>
    /// Presentation 层检查此标志以在正确时机替换 Stream。
    /// </summary>
    public bool NeedsStreamSwap { get; set; }

    /// <summary>
    /// 强制重置（用于系统停止）。
    /// </summary>
    public void Reset()
    {
        CurrentTrackId = null;
        PendingTrackId = null;
        IsFading = false;
        IsFadingOut = false;
        Volume = 0f;
        FadeElapsedMs = 0f;
    }
}

/// <summary>
/// 场景音频配置。每层一个可选 track ID。
/// </summary>
public sealed class SceneAudioConfig
{
    public string? TerrainTrack { get; init; }
    public string? WeatherTrack { get; init; }
    public string? TimeOfDayTrack { get; init; }
}

/// <summary>
/// 环境音三层纯逻辑引擎。管理各层 fade 状态。
/// 不依赖 Godot 运行时，可由 xUnit 直接测试。
/// </summary>
public sealed class AmbientLayerEngine
{
    public const float DefaultFadeMs = 2000f;

    private readonly AmbientLayerState[] _layers = new AmbientLayerState[3];

    public AmbientLayerEngine()
    {
        for (int i = 0; i < 3; i++)
            _layers[i] = new AmbientLayerState();
    }

    public AmbientLayerState GetLayer(AmbientLayer layer) => _layers[(int)layer];

    /// <summary>
    /// 设置指定层的 track。null/空 = 该层淡出。
    /// </summary>
    public void SetLayer(AmbientLayer layer, string? trackId, float fadeMs = DefaultFadeMs)
    {
        _layers[(int)layer].SetTrack(trackId, fadeMs);
    }

    /// <summary>
    /// 场景切换时设置三层：地形必换；天气/时辰仅在 track 变化时切换。
    /// </summary>
    public void OnSceneAudioConfig(SceneAudioConfig config, float fadeMs = DefaultFadeMs)
    {
        _layers[(int)AmbientLayer.Terrain].SetTrack(config.TerrainTrack, fadeMs);

        var weatherState = _layers[(int)AmbientLayer.Weather];
        if (config.WeatherTrack != weatherState.CurrentTrackId || weatherState.IsFading)
            weatherState.SetTrack(config.WeatherTrack, fadeMs);

        var todState = _layers[(int)AmbientLayer.TimeOfDay];
        if (config.TimeOfDayTrack != todState.CurrentTrackId || todState.IsFading)
            todState.SetTrack(config.TimeOfDayTrack, fadeMs);
    }

    /// <summary>
    /// 逐帧更新所有层的 fade 状态。
    /// </summary>
    public void Update(float deltaMs)
    {
        for (int i = 0; i < 3; i++)
            _layers[i].Update(deltaMs);
    }

    /// <summary>
    /// 是否有任何层正在 fade 中。
    /// </summary>
    public bool IsAnyFading =>
        _layers[0].IsFading || _layers[1].IsFading || _layers[2].IsFading;

    /// <summary>
    /// 全部停止。
    /// </summary>
    public void StopAll()
    {
        for (int i = 0; i < 3; i++)
            _layers[i].Reset();
    }
}
