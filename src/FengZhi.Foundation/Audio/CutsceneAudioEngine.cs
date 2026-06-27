namespace FengZhi.Foundation.Audio;

/// <summary>
/// 演出音频接管纯逻辑引擎。跟踪演出音频状态、战斗段播放位置保存/恢复。
/// 不依赖 Godot 运行时，可由 xUnit 直接测试。
/// </summary>
public sealed class CutsceneAudioEngine
{
    public const string CutsceneSourceId = "cutscene";
    public const float SkipFadeOutMs = 200f;

    private bool _isActive;
    private float _savedCombatPositionMs;
    private bool _hasSavedCombatPosition;
    private string? _cutsceneBgmTrackId;

    public bool IsActive => _isActive;
    public bool HasSavedCombatPosition => _hasSavedCombatPosition;
    public float SavedCombatPositionMs => _savedCombatPositionMs;
    public string? CutsceneBgmTrackId => _cutsceneBgmTrackId;

    /// <summary>
    /// 演出开始。可选保存战斗段播放位置（E3 场景）。
    /// </summary>
    public void Begin(string? bgmTrackId, float? combatPositionMs = null)
    {
        _isActive = true;
        _cutsceneBgmTrackId = bgmTrackId;

        if (combatPositionMs.HasValue)
        {
            _savedCombatPositionMs = combatPositionMs.Value;
            _hasSavedCombatPosition = true;
        }
    }

    /// <summary>
    /// 演出正常结束。返回是否需要恢复战斗位置。
    /// </summary>
    public CutsceneEndResult End()
    {
        _isActive = false;
        var result = new CutsceneEndResult(_hasSavedCombatPosition, _savedCombatPositionMs);
        ClearState();
        return result;
    }

    /// <summary>
    /// 演出被跳过。返回结果同 End，由 Presentation 层执行 200ms 淡出。
    /// </summary>
    public CutsceneEndResult Skip()
    {
        return End();
    }

    /// <summary>
    /// 重置状态（场景切换时）。
    /// </summary>
    public void Reset()
    {
        _isActive = false;
        ClearState();
    }

    private void ClearState()
    {
        _hasSavedCombatPosition = false;
        _savedCombatPositionMs = 0f;
        _cutsceneBgmTrackId = null;
    }
}

/// <summary>
/// 演出结束时的恢复信息。
/// </summary>
public readonly record struct CutsceneEndResult(
    bool ShouldRestoreCombatPosition,
    float CombatPositionMs
);
