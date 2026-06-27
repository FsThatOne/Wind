using System;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// 女主 Motif 叠加纯逻辑引擎。
/// 负责计算 motif 淡入淡出与 BGM duck 音量，不修改 BGM Override 栈。
/// </summary>
public sealed class MotifOverlayEngine
{
    public const float DefaultDuckRatio = 0.3f;
    public const float DefaultFadeMs = 800f;

    private readonly float _duckRatio;

    private MotifOverlayPhase _phase = MotifOverlayPhase.Idle;
    private string? _activeCharacterId;
    private string? _activeTrackId;
    private string? _pendingCharacterId;
    private string? _pendingTrackId;

    private float _bgmBaseVolume = 1f;
    private float _bgmVolume = 1f;
    private float _motifVolume;
    private float _elapsedMs;
    private float _durationMs;
    private float _startBgmVolume;
    private float _targetBgmVolume;
    private float _startMotifVolume;
    private float _targetMotifVolume;

    public MotifOverlayEngine(float duckRatio = DefaultDuckRatio)
    {
        _duckRatio = Math.Clamp(duckRatio, 0f, 1f);
    }

    public MotifOverlayPhase Phase => _phase;
    public bool IsMotifActive => _phase != MotifOverlayPhase.Idle;
    public string? ActiveCharacterId => _activeCharacterId;
    public string? ActiveTrackId => _activeTrackId;
    public string? PendingCharacterId => _pendingCharacterId;
    public string? PendingTrackId => _pendingTrackId;
    public float BgmBaseVolume => _bgmBaseVolume;
    public float BgmVolume => _bgmVolume;
    public float MotifVolume => _motifVolume;
    public float DuckRatio => _duckRatio;

    /// <summary>
    /// 更新当前 BGM 有效音量基准。Motif duck 始终叠加在该基准之上。
    /// </summary>
    public void SetBgmBaseVolume(float volume)
    {
        _bgmBaseVolume = Math.Clamp(volume, 0f, 1f);

        if (_phase == MotifOverlayPhase.Idle)
        {
            _bgmVolume = _bgmBaseVolume;
            return;
        }

        if (_phase == MotifOverlayPhase.Active || _phase == MotifOverlayPhase.SwitchingOut)
        {
            _bgmVolume = DuckedBgmVolume;
        }
        else
        {
            _targetBgmVolume = _phase == MotifOverlayPhase.FadingOut
                ? _bgmBaseVolume
                : DuckedBgmVolume;
        }
    }

    /// <summary>
    /// 开始播放角色 motif。若已有其他 motif，先淡出旧 motif，再淡入新 motif。
    /// </summary>
    public MotifPlayResult PlayMotif(string characterId, float fadeMs = DefaultFadeMs)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return MotifPlayResult.Ignored;

        string motifTrackId = ResolveMotifTrackId(characterId);

        if ((_phase == MotifOverlayPhase.Active || _phase == MotifOverlayPhase.FadingIn)
            && _activeCharacterId == characterId)
        {
            return MotifPlayResult.SameMotifContinue;
        }

        if (_phase != MotifOverlayPhase.Idle)
        {
            _pendingCharacterId = characterId;
            _pendingTrackId = motifTrackId;
            StartTransition(MotifOverlayPhase.SwitchingOut, fadeMs, DuckedBgmVolume, 0f);
            return MotifPlayResult.SwitchStarted;
        }

        _activeCharacterId = characterId;
        _activeTrackId = motifTrackId;
        StartTransition(MotifOverlayPhase.FadingIn, fadeMs, DuckedBgmVolume, 1f);
        return MotifPlayResult.FadeInStarted;
    }

    /// <summary>
    /// 停止当前 motif，恢复到 duck 前 BGM 有效音量。
    /// </summary>
    public MotifPlayResult StopMotif(float fadeMs = DefaultFadeMs)
    {
        if (_phase == MotifOverlayPhase.Idle)
            return MotifPlayResult.Ignored;

        _pendingCharacterId = null;
        _pendingTrackId = null;
        StartTransition(MotifOverlayPhase.FadingOut, fadeMs, _bgmBaseVolume, 0f);
        return MotifPlayResult.FadeOutStarted;
    }

    /// <summary>
    /// 逐帧推进 motif/duck 过渡。返回当前音量快照。
    /// </summary>
    public MotifOverlayFrame Update(float deltaMs)
    {
        if (_phase == MotifOverlayPhase.Idle || _phase == MotifOverlayPhase.Active)
        {
            return Snapshot();
        }

        _elapsedMs += Math.Max(0f, deltaMs);
        float t = _durationMs <= 0f ? 1f : Math.Min(_elapsedMs / _durationMs, 1f);

        _bgmVolume = Lerp(_startBgmVolume, _targetBgmVolume, t);
        _motifVolume = Lerp(_startMotifVolume, _targetMotifVolume, t);

        if (t >= 1f)
            CompleteTransition();

        return Snapshot();
    }

    /// <summary>
    /// 强制清除 motif 状态，恢复到当前 BGM 基准音量。
    /// </summary>
    public void Reset()
    {
        _phase = MotifOverlayPhase.Idle;
        _activeCharacterId = null;
        _activeTrackId = null;
        _pendingCharacterId = null;
        _pendingTrackId = null;
        _bgmVolume = _bgmBaseVolume;
        _motifVolume = 0f;
        _elapsedMs = 0f;
        _durationMs = 0f;
        _startBgmVolume = _bgmBaseVolume;
        _targetBgmVolume = _bgmBaseVolume;
        _startMotifVolume = 0f;
        _targetMotifVolume = 0f;
    }

    /// <summary>
    /// 根据角色 id 解析 motif track id。资产加载由 Presentation 层完成。
    /// </summary>
    public static string ResolveMotifTrackId(string characterId)
    {
        return $"{characterId}_motif";
    }

    private float DuckedBgmVolume => _bgmBaseVolume * _duckRatio;

    private void StartTransition(MotifOverlayPhase phase, float durationMs, float targetBgmVolume, float targetMotifVolume)
    {
        _phase = phase;
        _elapsedMs = 0f;
        _durationMs = Math.Max(0f, durationMs);
        _startBgmVolume = _bgmVolume;
        _targetBgmVolume = targetBgmVolume;
        _startMotifVolume = _motifVolume;
        _targetMotifVolume = targetMotifVolume;
    }

    private void CompleteTransition()
    {
        switch (_phase)
        {
            case MotifOverlayPhase.FadingIn:
                _phase = MotifOverlayPhase.Active;
                _bgmVolume = DuckedBgmVolume;
                _motifVolume = 1f;
                break;
            case MotifOverlayPhase.FadingOut:
                _phase = MotifOverlayPhase.Idle;
                _activeCharacterId = null;
                _activeTrackId = null;
                _bgmVolume = _bgmBaseVolume;
                _motifVolume = 0f;
                break;
            case MotifOverlayPhase.SwitchingOut:
                _activeCharacterId = _pendingCharacterId;
                _activeTrackId = _pendingTrackId;
                _pendingCharacterId = null;
                _pendingTrackId = null;
                _motifVolume = 0f;
                StartTransition(MotifOverlayPhase.FadingIn, _durationMs, DuckedBgmVolume, 1f);
                break;
        }
    }

    private MotifOverlayFrame Snapshot()
    {
        return new MotifOverlayFrame(
            ActiveCharacterId: _activeCharacterId,
            ActiveTrackId: _activeTrackId,
            PendingCharacterId: _pendingCharacterId,
            PendingTrackId: _pendingTrackId,
            BgmVolume: _bgmVolume,
            MotifVolume: _motifVolume,
            Phase: _phase
        );
    }

    private static float Lerp(float from, float to, float t)
    {
        return from + (to - from) * t;
    }
}

public enum MotifOverlayPhase
{
    Idle,
    FadingIn,
    Active,
    SwitchingOut,
    FadingOut
}

public enum MotifPlayResult
{
    FadeInStarted,
    FadeOutStarted,
    SwitchStarted,
    SameMotifContinue,
    Ignored
}

public readonly record struct MotifOverlayFrame(
    string? ActiveCharacterId,
    string? ActiveTrackId,
    string? PendingCharacterId,
    string? PendingTrackId,
    float BgmVolume,
    float MotifVolume,
    MotifOverlayPhase Phase
);
