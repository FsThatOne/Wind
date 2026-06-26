using Godot;
using FengZhi.Foundation.Audio;
using FengZhi.Foundation.StateMachine;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// 音频系统入口 Autoload。持有音频 FSM，管理状态转换和总线衰减。
/// 注册为 Autoload: Project → AutoLoad → AudioDirector (res://feng-zhi/scripts/audio/AudioDirector.cs)
/// </summary>
public partial class AudioDirector : Node
{
    private StateMachine<AudioState> _fsm = null!;
    private readonly Stack<AudioState> _returnStack = new();

    public AudioState CurrentState => _fsm.CurrentState;

    public override void _Ready()
    {
        _fsm = AudioStateMachineFactory.Create();

        _fsm.OnStateEnter += OnStateEnter;

        InitializeBusLayout();
        ApplyAttenuation(AudioState.Exploration);
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
        var (bgm, ambient, sfx) = AudioStateAttenuation.GetAll(state);

        SetBusAttenuation(AudioBusLayout.Bgm, bgm);
        SetBusAttenuation(AudioBusLayout.Ambient, ambient);
        SetBusAttenuation(AudioBusLayout.Sfx, sfx);
    }

    private static void SetBusAttenuation(string busName, float linearVolume)
    {
        int busIdx = AudioServer.GetBusIndex(busName);
        if (busIdx < 0) return;

        float db = linearVolume <= 0f ? -80f : Mathf.LinearToDb(linearVolume);
        AudioServer.SetBusVolumeDb(busIdx, db);
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
}
