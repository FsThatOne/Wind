using System;
using System.Collections.Generic;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// BGM crossfade 纯逻辑引擎。管理 Override 栈、等功率 crossfade 状态和同曲续播检测。
/// 不依赖 Godot 运行时，可由 xUnit 直接测试。
/// </summary>
public sealed class BgmCrossfadeEngine
{
    public const string SilenceTrackId = "SILENCE";
    private const float SilentDb = -80f;

    private readonly Stack<BgmEntry> _overrideStack = new();
    private readonly int _maxStackDepth;

    private string _currentTrackId = string.Empty;
    private CrossfadeState _crossfadeState = CrossfadeState.Idle;

    private float _fadeOutElapsedMs;
    private float _fadeOutDurationMs;
    private float _fadeInElapsedMs;
    private float _fadeInDurationMs;
    private string _pendingTrackId = string.Empty;

    public BgmCrossfadeEngine(int maxStackDepth = AudioBusLayout.BgmOverrideStackMaxDepth)
    {
        _maxStackDepth = maxStackDepth;
    }

    public string CurrentTrackId => _currentTrackId;
    public string PendingTrackId => _pendingTrackId;
    public bool IsCrossfading => _crossfadeState == CrossfadeState.Fading;
    public int StackDepth => _overrideStack.Count;

    /// <summary>
    /// 直接播放 BGM（场景切换用）。清空 Override 栈并 crossfade 到新曲。
    /// 返回 PlayResult 指示实际行为。
    /// </summary>
    public PlayResult PlayBgm(string trackId, float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        _overrideStack.Clear();

        if (string.IsNullOrEmpty(trackId))
            return PlayResult.Ignored;

        if (trackId == _currentTrackId && _crossfadeState == CrossfadeState.Idle)
            return PlayResult.SameTrackContinue;

        StartCrossfade(trackId, fadeOutMs, fadeInMs);
        return trackId == SilenceTrackId ? PlayResult.FadeToSilence : PlayResult.Crossfading;
    }

    /// <summary>
    /// 将 BGM 压入 Override 栈（战斗/演出进入时）。栈溢出替换栈顶。
    /// </summary>
    public PlayResult PushBgm(string trackId, AudioState state, float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        if (_overrideStack.Count >= _maxStackDepth)
        {
            _overrideStack.Pop();
        }

        _overrideStack.Push(new BgmEntry(trackId, state));

        if (trackId == _currentTrackId && _crossfadeState == CrossfadeState.Idle)
            return PlayResult.SameTrackContinue;

        StartCrossfade(trackId, fadeOutMs, fadeInMs);
        return trackId == SilenceTrackId ? PlayResult.FadeToSilence : PlayResult.Crossfading;
    }

    /// <summary>
    /// 弹出 Override 栈顶，恢复前一首 BGM。
    /// </summary>
    public PlayResult PopBgm(float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        if (_overrideStack.Count == 0)
            return PlayResult.Ignored;

        _overrideStack.Pop();

        if (_overrideStack.Count == 0)
        {
            StartCrossfade(string.Empty, fadeOutMs, 0f);
            return PlayResult.FadeToSilence;
        }

        var prev = _overrideStack.Peek();

        if (prev.TrackId == _currentTrackId && _crossfadeState == CrossfadeState.Idle)
            return PlayResult.SameTrackContinue;

        StartCrossfade(prev.TrackId, fadeOutMs, fadeInMs);
        return prev.TrackId == SilenceTrackId ? PlayResult.FadeToSilence : PlayResult.Crossfading;
    }

    /// <summary>
    /// 逐帧更新 crossfade 进度。返回当前帧的音量状态。
    /// </summary>
    public FrameVolumes Update(float deltaMs)
    {
        if (_crossfadeState == CrossfadeState.Idle)
        {
            bool isSilent = _currentTrackId is "" or SilenceTrackId;
            return new FrameVolumes(
                FadeOutVolume: isSilent ? 0f : 1f,
                FadeInVolume: 0f,
                FadeOutComplete: true,
                FadeInComplete: true,
                NewTrackReady: false
            );
        }

        _fadeOutElapsedMs += deltaMs;
        _fadeInElapsedMs += deltaMs;

        float fadeOutLinear = _fadeOutDurationMs > 0f
            ? EqualPowerFadeOut(Math.Min(_fadeOutElapsedMs / _fadeOutDurationMs, 1f))
            : 0f;

        bool isSilenceTarget = _pendingTrackId is "" or SilenceTrackId;
        float fadeInLinear = 0f;
        if (!isSilenceTarget && _fadeInDurationMs > 0f)
        {
            fadeInLinear = EqualPowerFadeIn(Math.Min(_fadeInElapsedMs / _fadeInDurationMs, 1f));
        }

        bool fadeOutDone = _fadeOutElapsedMs >= _fadeOutDurationMs;
        bool fadeInDone = isSilenceTarget || _fadeInElapsedMs >= _fadeInDurationMs;

        if (fadeOutDone && fadeInDone)
        {
            _crossfadeState = CrossfadeState.Idle;
            _currentTrackId = _pendingTrackId;
        }

        return new FrameVolumes(
            FadeOutVolume: fadeOutLinear,
            FadeInVolume: fadeInLinear,
            FadeOutComplete: fadeOutDone,
            FadeInComplete: fadeInDone,
            NewTrackReady: _crossfadeState == CrossfadeState.Idle
        );
    }

    /// <summary>
    /// 等功率淡出: cos(t × π/2)，t ∈ [0,1]
    /// </summary>
    public static float EqualPowerFadeOut(float t)
    {
        return MathF.Cos(t * MathF.PI / 2f);
    }

    /// <summary>
    /// 等功率淡入: sin(t × π/2)，t ∈ [0,1]
    /// </summary>
    public static float EqualPowerFadeIn(float t)
    {
        return MathF.Sin(t * MathF.PI / 2f);
    }

    /// <summary>
    /// 线性音量转 dB。
    /// </summary>
    public static float LinearToDb(float linear)
    {
        return linear <= 0f ? SilentDb : 20f * MathF.Log10(linear);
    }

    private void StartCrossfade(string newTrackId, float fadeOutMs, float fadeInMs)
    {
        _crossfadeState = CrossfadeState.Fading;
        _pendingTrackId = newTrackId;
        _fadeOutElapsedMs = 0f;
        _fadeOutDurationMs = fadeOutMs;
        _fadeInElapsedMs = 0f;
        _fadeInDurationMs = fadeInMs;
    }

    private enum CrossfadeState
    {
        Idle,
        Fading
    }
}

/// <summary>
/// PlayBgm/PushBgm/PopBgm 调用的返回值，指示 Presentation 层应执行的行为。
/// </summary>
public enum PlayResult
{
    Crossfading,
    FadeToSilence,
    SameTrackContinue,
    Ignored
}

/// <summary>
/// 单帧 crossfade 音量快照。
/// </summary>
public readonly record struct FrameVolumes(
    float FadeOutVolume,
    float FadeInVolume,
    bool FadeOutComplete,
    bool FadeInComplete,
    bool NewTrackReady
);
