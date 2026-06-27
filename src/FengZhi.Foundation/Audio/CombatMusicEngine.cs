using System;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// 战斗 6 段水平分层音乐状态。
/// </summary>
public enum CombatSegment
{
    Prep,
    Clash,
    Advantage,
    Disadvantage,
    Desperation,
    Finisher
}

/// <summary>
/// 战斗音乐配置：BPM、bar_length_ms、6 段 track path。
/// </summary>
public sealed class CombatMusicConfig
{
    public int Bpm { get; init; } = 120;
    public float BarLengthMs { get; init; } = 2000f;
    public string PrepTrack { get; init; } = string.Empty;
    public string ClashTrack { get; init; } = string.Empty;
    public string AdvantageTrack { get; init; } = string.Empty;
    public string DisadvantageTrack { get; init; } = string.Empty;
    public string DesperationTrack { get; init; } = string.Empty;
    public string FinisherTrack { get; init; } = string.Empty;

    public string GetTrack(CombatSegment segment) => segment switch
    {
        CombatSegment.Prep => PrepTrack,
        CombatSegment.Clash => ClashTrack,
        CombatSegment.Advantage => AdvantageTrack,
        CombatSegment.Disadvantage => DisadvantageTrack,
        CombatSegment.Desperation => DesperationTrack,
        CombatSegment.Finisher => FinisherTrack,
        _ => string.Empty
    };

    /// <summary>
    /// 从 BPM 和拍号计算 bar_length_ms。默认 4/4 拍。
    /// </summary>
    public static float CalculateBarLengthMs(int bpm, int beatsPerBar = 4)
    {
        if (bpm <= 0) return 2000f;
        return beatsPerBar * 60_000f / bpm;
    }
}

/// <summary>
/// 段落切换请求结果。
/// </summary>
public enum SegmentSwitchResult
{
    /// <summary>切换已安排，等待 bar boundary。</summary>
    Scheduled,
    /// <summary>Finisher 立即切换（跳过 bar boundary 等待）。</summary>
    Immediate,
    /// <summary>已在目标段落，无需切换。</summary>
    AlreadyInSegment,
    /// <summary>无有效配置，忽略。</summary>
    NoConfig
}

/// <summary>
/// 自适应战斗音乐纯逻辑引擎。管理段落判定、小节线对齐切换调度。
/// 不依赖 Godot 运行时，可由 xUnit 直接测试。
/// </summary>
public sealed class CombatMusicEngine
{
    public const float BarBoundaryToleranceMs = 100f;
    public const float CombatFadeOutMs = 500f;
    public const float CombatFadeInMs = 300f;
    public const float BattleEndFadeOutMs = 2000f;

    private CombatMusicConfig? _config;
    private CombatSegment _currentSegment = CombatSegment.Prep;
    private CombatSegment? _pendingSegment;
    private float _playbackPositionMs;
    private bool _isActive;

    public CombatSegment CurrentSegment => _currentSegment;
    public CombatSegment? PendingSegment => _pendingSegment;
    public bool IsActive => _isActive;
    public CombatMusicConfig? Config => _config;

    /// <summary>
    /// 开始战斗音乐。设置配置并激活引擎。
    /// </summary>
    public string? StartCombat(CombatMusicConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _currentSegment = CombatSegment.Prep;
        _pendingSegment = null;
        _playbackPositionMs = 0f;
        _isActive = true;
        return config.PrepTrack;
    }

    /// <summary>
    /// 结束战斗音乐。返回 fade out 时长。
    /// </summary>
    public float StopCombat()
    {
        _isActive = false;
        _pendingSegment = null;
        _config = null;
        return BattleEndFadeOutMs;
    }

    /// <summary>
    /// 更新播放位置（由 Presentation 层每帧调用）。
    /// </summary>
    public void UpdatePlaybackPosition(float positionMs)
    {
        _playbackPositionMs = positionMs;
    }

    /// <summary>
    /// F3 公式：按优先级从高到低判定当前战斗段落。
    /// </summary>
    public static CombatSegment EvaluateCombatState(
        float hpRatio,
        float staminaRatio,
        int advantageStreak,
        bool finisherTriggered,
        bool isInActionWindow = false,
        int counterStreak = 0)
    {
        if (finisherTriggered)
            return CombatSegment.Finisher;
        if (hpRatio < 0.15f && staminaRatio < 0.20f)
            return CombatSegment.Desperation;
        if (hpRatio < 0.30f || counterStreak >= 2)
            return CombatSegment.Disadvantage;
        if (advantageStreak >= 2)
            return CombatSegment.Advantage;
        if (isInActionWindow)
            return CombatSegment.Clash;
        return CombatSegment.Prep;
    }

    /// <summary>
    /// 计算到下一个小节线的剩余时间（ms）。
    /// </summary>
    public float GetMsUntilNextBar()
    {
        if (_config == null || _config.BarLengthMs <= 0f)
            return 0f;
        float elapsed = _playbackPositionMs % _config.BarLengthMs;
        return _config.BarLengthMs - elapsed;
    }

    /// <summary>
    /// 请求切换到目标段落。返回切换结果。
    /// Finisher 跳过 bar boundary 等待直接切换。
    /// </summary>
    public SegmentSwitchResult RequestSegmentSwitch(CombatSegment target)
    {
        if (!_isActive || _config == null)
            return SegmentSwitchResult.NoConfig;

        if (target == _currentSegment && _pendingSegment == null)
            return SegmentSwitchResult.AlreadyInSegment;

        if (target == CombatSegment.Finisher)
        {
            _pendingSegment = null;
            _currentSegment = target;
            return SegmentSwitchResult.Immediate;
        }

        float remainMs = GetMsUntilNextBar();
        if (remainMs <= BarBoundaryToleranceMs)
        {
            _pendingSegment = null;
            _currentSegment = target;
            return SegmentSwitchResult.Immediate;
        }

        _pendingSegment = target;
        return SegmentSwitchResult.Scheduled;
    }

    /// <summary>
    /// 检查 pending 切换是否可以执行（到达 bar boundary）。
    /// 由 Presentation 层逐帧调用。返回应切换到的段落，或 null 表示无需切换。
    /// </summary>
    public CombatSegment? TryCompletePendingSwitch()
    {
        if (_pendingSegment == null || _config == null)
            return null;

        float remainMs = GetMsUntilNextBar();
        if (remainMs <= BarBoundaryToleranceMs || remainMs >= _config.BarLengthMs - BarBoundaryToleranceMs)
        {
            var target = _pendingSegment.Value;
            _currentSegment = target;
            _pendingSegment = null;
            return target;
        }

        return null;
    }

    /// <summary>
    /// 获取目标段落的 track ID。
    /// </summary>
    public string? GetSegmentTrack(CombatSegment segment)
    {
        return _config?.GetTrack(segment);
    }
}
