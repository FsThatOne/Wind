using System.Collections.Generic;
using Godot;
using FengZhi.Foundation.Audio;
using FengZhi.Foundation.SaveSystem;
using FengZhi.Foundation.StateMachine;
using AudioBusLayout = FengZhi.Foundation.Audio.AudioBusLayout;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// 音频系统入口 Autoload。持有音频 FSM、BgmManager、AmbientManager、SfxManager，
/// 管理状态转换、总线衰减和场景音频切换。
/// 注册为 Autoload: Project → AutoLoad → AudioDirector (res://scripts/audio/AudioDirector.cs)
/// </summary>
public partial class AudioDirector : Node, ISaveable
{
    private StateMachine<AudioState> _fsm = null!;
    private readonly Stack<AudioState> _returnStack = new();
    private readonly AudioVolumeController _volumeController = new(new GodotAudioBusVolumeWriter());

    private BgmManager _bgmManager = null!;
    private AmbientManager _ambientManager = null!;
    private SfxManager _sfxManager = null!;

    public AudioState CurrentState => _fsm.CurrentState;
    public BgmManager Bgm => _bgmManager;
    public AmbientManager Ambient => _ambientManager;
    public SfxManager Sfx => _sfxManager;
    public AudioVolumeSettings VolumeSettings => _volumeController.Settings;
    public string SaveKey => _volumeController.SaveKey;

    public override void _Ready()
    {
        _fsm = AudioStateMachineFactory.Create();
        _fsm.OnStateEnter += OnStateEnter;

        InitializeBusLayout();

        _bgmManager = new BgmManager { Name = "BgmManager" };
        AddChild(_bgmManager);

        _ambientManager = new AmbientManager { Name = "AmbientManager" };
        AddChild(_ambientManager);

        _sfxManager = new SfxManager { Name = "SfxManager" };
        AddChild(_sfxManager);

        ApplyAttenuation(AudioState.Exploration);
    }

    /// <summary>
    /// 场景进入时调用：设置场景 BGM 和地形环境音。
    /// BGM 自动 crossfade（同曲续播检测由 BgmCrossfadeEngine 处理），
    /// 地形环境音独立淡入淡出。天气/时辰层保持不变。
    /// - bgmId 为空/null：不切换 BGM，保持上一场景的 BGM 继续播放（同区域子场景）
    /// - bgmId = "SILENCE"：刻意留白，淡出到静音
    /// - bgmId = 其他值：crossfade 到该 BGM
    /// - terrainAmbientId 为空/null：不切换地形环境音；否则切换到指定环境音
    /// </summary>
    public void ApplySceneAudio(string bgmId, string terrainAmbientId)
    {
        if (!string.IsNullOrEmpty(bgmId))
            _bgmManager.PlayBgm(bgmId);

        if (!string.IsNullOrEmpty(terrainAmbientId))
            _ambientManager.SetLayer(AmbientLayer.Terrain, terrainAmbientId);
    }

    /// <summary>
    /// 战斗/演出进入：将新 BGM 压入 Override 栈，退出时调用 PopOverrideBgm 恢复。
    /// </summary>
    public void PushOverrideBgm(string trackId, AudioState state)
    {
        _bgmManager.PushBgm(trackId, state);
    }

    /// <summary>
    /// 战斗/演出退出：弹出 Override 栈顶，恢复前一首 BGM。
    /// </summary>
    public void PopOverrideBgm()
    {
        _bgmManager.PopBgm();
    }

    /// <summary>
    /// 战斗内段落切换（不修改 Override 栈）。
    /// </summary>
    public void CrossfadeCombatSegment(string trackId, float fadeOutMs = 500f, float fadeInMs = 300f)
    {
        _bgmManager.CrossfadeBgm(trackId, fadeOutMs, fadeInMs);
    }

    /// <summary>
    /// 设置天气环境音层。天气系统切换天气时调用。传 null 停掉天气层。
    /// </summary>
    public void SetWeatherAmbient(string? trackId)
    {
        _ambientManager.SetLayer(AmbientLayer.Weather, trackId);
    }

    /// <summary>
    /// 设置时辰环境音层。时辰系统切换时辰时调用。传 null 停掉时辰层。
    /// </summary>
    public void SetTimeOfDayAmbient(string? trackId)
    {
        _ambientManager.SetLayer(AmbientLayer.TimeOfDay, trackId);
    }

    /// <summary>
    /// 播放女主 motif overlay（不进入 BGM Override 栈，作为叠加层）。
    /// </summary>
    public void PlayMotif(string characterId, float fadeMs = MotifOverlayEngine.DefaultFadeMs)
    {
        _bgmManager.PlayMotif(characterId, fadeMs);
    }

    /// <summary>
    /// 停止女主 motif overlay，恢复场景 BGM 音量。
    /// </summary>
    public void StopMotif(float fadeMs = MotifOverlayEngine.DefaultFadeMs)
    {
        _bgmManager.StopMotif(fadeMs);
    }

    /// <summary>
    /// 设置用户音量（0.0-1.0），并立即刷新对应 AudioServer 总线。
    /// </summary>
    public void SetVolume(AudioVolumeTrack track, float value01)
    {
        _volumeController.SetVolume(track, value01);
    }

    /// <summary>
    /// 读取用户音量配置（0.0-1.0）。
    /// </summary>
    public float GetVolume(AudioVolumeTrack track)
    {
        return _volumeController.GetVolume(track);
    }

    /// <inheritdoc />
    public SaveSnapshot Serialize()
    {
        return _volumeController.Serialize();
    }

    /// <inheritdoc />
    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _volumeController.Deserialize(snapshot, version);
    }

    /// <summary>
    /// 触发状态转换。Push 型触发器压栈当前状态；Pop 型弹栈回退；Direct 型清栈直接转换。
    /// </summary>
    public bool Trigger(string trigger)
    {
        if (trigger is AudioTriggers.EndCutscene or AudioTriggers.EndDialogue or AudioTriggers.CloseMenu)
        {
            if (_returnStack.Count == 0)
                return _fsm.TryTransition(trigger);
            var target = _returnStack.Pop();
            return _fsm.ForceTransition(target, trigger);
        }

        if (trigger is AudioTriggers.StartCutscene or AudioTriggers.StartDialogue or AudioTriggers.OpenMenu)
        {
            _returnStack.Push(_fsm.CurrentState);
            return _fsm.TryTransition(trigger);
        }

        _returnStack.Clear();
        return _fsm.TryTransition(trigger);
    }

    /// <summary>
    /// 强制切换到指定状态（跳过规则检查）。主线剧情强制推进时使用。
    /// </summary>
    public bool ForceState(AudioState target, string? reason = null)
    {
        _returnStack.Clear();
        return _fsm.ForceTransition(target, reason);
    }

    private void OnStateEnter(AudioState from, AudioState to)
    {
        ApplyAttenuation(to);
    }

    private void ApplyAttenuation(AudioState state)
    {
        _volumeController.ApplyState(state);
    }

    private static void InitializeBusLayout()
    {
        EnsureBusExists(AudioBusLayout.Bgm, AudioBusLayout.Master);
        EnsureBusExists(AudioBusLayout.BgmMain, AudioBusLayout.Bgm);
        EnsureBusExists(AudioBusLayout.BgmCrossfade, AudioBusLayout.Bgm);
        EnsureBusExists(AudioBusLayout.Ambient, AudioBusLayout.Master);
        EnsureBusExists(AudioBusLayout.AmbientTerrain, AudioBusLayout.Ambient);
        EnsureBusExists(AudioBusLayout.AmbientWeather, AudioBusLayout.Ambient);
        EnsureBusExists(AudioBusLayout.AmbientTimeOfDay, AudioBusLayout.Ambient);
        EnsureBusExists(AudioBusLayout.Sfx, AudioBusLayout.Master);
        EnsureBusExists(AudioBusLayout.SfxPool, AudioBusLayout.Sfx);
    }

    private static void EnsureBusExists(string busName, string sendTo)
    {
        if (AudioServer.GetBusIndex(busName) >= 0) return;

        int newIdx = AudioServer.BusCount;
        AudioServer.AddBus(newIdx);
        AudioServer.SetBusName(newIdx, busName);

        int sendIdx = AudioServer.GetBusIndex(sendTo);
        if (sendIdx >= 0)
            AudioServer.SetBusSend(newIdx, sendTo);
    }

    private static readonly string[] AudioExtensions = { ".ogg", ".mp3" };

    /// <summary>
    /// 按优先级加载音频流：优先 .ogg（无缝循环更好），找不到则回退到 .mp3。
    /// directory 是相对于 res://assets/audio/ 的子目录（如 "bgm"、"ambient"、"sfx"），
    /// trackId 是不含扩展名的文件名。找不到时返回 null。
    /// </summary>
    public static AudioStream? LoadAudioStream(string directory, string trackId)
    {
        foreach (string ext in AudioExtensions)
        {
            string path = $"res://assets/audio/{directory}/{trackId}{ext}";
            if (ResourceLoader.Exists(path))
                return GD.Load<AudioStream>(path);
        }
        return null;
    }

    private sealed class GodotAudioBusVolumeWriter : IAudioBusVolumeWriter
    {
        public void SetBusVolumeDb(string busName, float volumeDb)
        {
            int busIdx = AudioServer.GetBusIndex(busName);
            if (busIdx < 0) return;

            AudioServer.SetBusVolumeDb(busIdx, volumeDb);
        }
    }
}
