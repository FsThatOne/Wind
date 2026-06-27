using Godot;
using FengZhi.Foundation.Audio;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// 自适应战斗音乐控制器。逐帧驱动 CombatMusicEngine，
/// 通过 BgmManager 执行 crossfade 切换。作为 AudioDirector 的子节点运行。
/// </summary>
public partial class CombatMusicController : Node
{
    private CombatMusicEngine _engine = new();
    private BgmManager _bgmManager = null!;
    private AudioStreamPlayer? _combatPlayer;

    public CombatMusicEngine Engine => _engine;

    public override void _Ready()
    {
        _bgmManager = GetParent().GetNode<BgmManager>("BgmManager");
    }

    public override void _Process(double delta)
    {
        if (!_engine.IsActive) return;

        if (_combatPlayer != null && _combatPlayer.Playing)
        {
            float posMs = (float)(_combatPlayer.GetPlaybackPosition() * 1000.0);
            _engine.UpdatePlaybackPosition(posMs);
        }

        var switchTarget = _engine.TryCompletePendingSwitch();
        if (switchTarget != null)
        {
            ExecuteSegmentSwitch(switchTarget.Value);
        }
    }

    /// <summary>
    /// 进入战斗，启动自适应音乐。
    /// </summary>
    public void StartCombat(CombatMusicConfig config)
    {
        var prepTrack = _engine.StartCombat(config);
        if (prepTrack == null) return;

        _bgmManager.PushBgm(prepTrack, AudioState.Combat,
            CombatMusicEngine.CombatFadeOutMs, CombatMusicEngine.CombatFadeInMs);
        _combatPlayer = _bgmManager.ActivePlayer;
    }

    /// <summary>
    /// 战斗状态更新时调用，重新评估段落。
    /// </summary>
    public void OnCombatStateChanged(
        float hpRatio,
        float staminaRatio,
        int advantageStreak,
        bool finisherTriggered,
        bool isInActionWindow = false,
        int counterStreak = 0)
    {
        if (!_engine.IsActive) return;

        var newSegment = CombatMusicEngine.EvaluateCombatState(
            hpRatio, staminaRatio, advantageStreak, finisherTriggered,
            isInActionWindow, counterStreak);

        var result = _engine.RequestSegmentSwitch(newSegment);
        if (result == SegmentSwitchResult.Immediate)
        {
            ExecuteSegmentSwitch(newSegment);
        }
    }

    /// <summary>
    /// 战斗结束，淡出战斗 BGM 并恢复场景 BGM。
    /// </summary>
    public void StopCombat()
    {
        float fadeOutMs = _engine.StopCombat();
        _bgmManager.PopBgm(fadeOutMs);
        _combatPlayer = null;
    }

    private void ExecuteSegmentSwitch(CombatSegment target)
    {
        var trackId = _engine.GetSegmentTrack(target);
        if (string.IsNullOrEmpty(trackId)) return;

        _bgmManager.CrossfadeBgm(trackId,
            CombatMusicEngine.CombatFadeOutMs, CombatMusicEngine.CombatFadeInMs);
        _combatPlayer = _bgmManager.ActivePlayer;
    }

    private AudioStreamPlayer? GetActivePlayer()
    {
        return _bgmManager.ActivePlayer;
    }
}
