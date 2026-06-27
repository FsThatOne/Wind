using Godot;
using FengZhi.Foundation.Audio;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// 演出音频接管控制器。协调 BgmManager 和 SfxManager 实现演出 BGM 压栈、
/// 演出 SFX 播放（P0 bypass cooldown）、跳过时 200ms 淡出恢复。
/// 作为 AudioDirector 的子节点运行。
/// </summary>
public partial class CutsceneAudioController : Node
{
    private BgmManager _bgmManager = null!;
    private SfxManager _sfxManager = null!;
    private CombatMusicController _combatController = null!;
    private AudioDirector _audioDirector = null!;
    private readonly CutsceneAudioEngine _engine = new();

    public CutsceneAudioEngine Engine => _engine;

    public override void _Ready()
    {
        var parent = GetParent();
        _bgmManager = parent.GetNode<BgmManager>("BgmManager");
        _sfxManager = parent.GetNode<SfxManager>("SfxManager");
        _combatController = parent.GetNode<CombatMusicController>("CombatMusicController");
        _audioDirector = parent as AudioDirector ?? parent.GetParent<AudioDirector>();
    }

    /// <summary>
    /// 演出开始播放 BGM。压入 Override 栈，FSM 转 Cutscene 状态。
    /// </summary>
    public void CutscenePlayBgm(string trackId, float fadeInMs = 800f)
    {
        float? combatPos = null;
        if (_audioDirector.CurrentState == AudioState.Combat)
        {
            var combatPlayer = _bgmManager.ActivePlayer;
            if (combatPlayer != null && combatPlayer.Playing)
                combatPos = (float)(combatPlayer.GetPlaybackPosition() * 1000.0);
        }

        _engine.Begin(trackId, combatPos);
        _audioDirector.Trigger(AudioTriggers.StartCutscene);
        _bgmManager.PushBgm(trackId, AudioState.Cutscene, fadeOutMs: 500f, fadeInMs: fadeInMs);
    }

    /// <summary>
    /// 演出播放 SFX。使用 P0 优先级，bypass cooldown。
    /// </summary>
    public PlaySfxResult CutscenePlaySfx(string sfxId)
    {
        return _sfxManager.PlaySfx(sfxId, SfxPriority.P0, CutsceneAudioEngine.CutsceneSourceId, bypassCooldown: true);
    }

    /// <summary>
    /// 演出被跳过。200ms 淡出所有演出音频，BGM 栈回退，FSM 恢复。
    /// </summary>
    public void OnCutsceneSkipped()
    {
        if (!_engine.IsActive) return;

        var result = _engine.Skip();

        _sfxManager.FadeOutBySource(CutsceneAudioEngine.CutsceneSourceId, CutsceneAudioEngine.SkipFadeOutMs);
        _bgmManager.PopBgm(fadeOutMs: CutsceneAudioEngine.SkipFadeOutMs, fadeInMs: 300f);
        _audioDirector.Trigger(AudioTriggers.EndCutscene);

        if (result.ShouldRestoreCombatPosition)
            RestoreCombatPosition(result.CombatPositionMs);
    }

    /// <summary>
    /// 演出正常结束。正常淡出 BGM，FSM 恢复。
    /// </summary>
    public void OnCutsceneEnded()
    {
        if (!_engine.IsActive) return;

        var result = _engine.End();

        _sfxManager.FadeOutBySource(CutsceneAudioEngine.CutsceneSourceId, 100f);
        _bgmManager.PopBgm();
        _audioDirector.Trigger(AudioTriggers.EndCutscene);

        if (result.ShouldRestoreCombatPosition)
            RestoreCombatPosition(result.CombatPositionMs);
    }

    private void RestoreCombatPosition(float positionMs)
    {
        var player = _bgmManager.IncomingPlayer;
        if (player != null && player.Playing)
            player.Seek((float)(positionMs / 1000.0));
    }
}
