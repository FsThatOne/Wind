using FengZhi.Foundation.StateMachine;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// 创建并配置音频状态机（含完整转换规则）。ADR-0009 + ADR-0008。
/// "返回前态"转换（EndCutscene/EndDialogue/CloseMenu）注册默认目标 Exploration，
/// 调用方应使用 ForceTransition 到实际 _previousState 以实现正确的"返回"。
/// </summary>
public static class AudioStateMachineFactory
{
    public static StateMachine<AudioState> Create()
    {
        var fsm = new StateMachine<AudioState>(AudioState.Exploration);

        // 注册所有状态
        fsm.RegisterState(AudioState.Combat);
        fsm.RegisterState(AudioState.Cutscene);
        fsm.RegisterState(AudioState.Dialogue);
        fsm.RegisterState(AudioState.Menu);
        fsm.RegisterState(AudioState.Silence);

        // ─── 战斗 ───
        fsm.AddTransition(AudioState.Exploration, AudioTriggers.EnterCombat, AudioState.Combat);
        fsm.AddTransition(AudioState.Combat, AudioTriggers.ExitCombat, AudioState.Exploration);

        // ─── 演出（从任意非 Silence 状态） ───
        fsm.AddTransition(AudioState.Exploration, AudioTriggers.StartCutscene, AudioState.Cutscene);
        fsm.AddTransition(AudioState.Combat, AudioTriggers.StartCutscene, AudioState.Cutscene);
        fsm.AddTransition(AudioState.Dialogue, AudioTriggers.StartCutscene, AudioState.Cutscene);
        fsm.AddTransition(AudioState.Menu, AudioTriggers.StartCutscene, AudioState.Cutscene);
        fsm.AddTransition(AudioState.Cutscene, AudioTriggers.EndCutscene, AudioState.Exploration);

        // ─── 对话（从 Exploration/Combat） ───
        fsm.AddTransition(AudioState.Exploration, AudioTriggers.StartDialogue, AudioState.Dialogue);
        fsm.AddTransition(AudioState.Combat, AudioTriggers.StartDialogue, AudioState.Dialogue);
        fsm.AddTransition(AudioState.Dialogue, AudioTriggers.EndDialogue, AudioState.Exploration);

        // ─── 菜单（从 Exploration/Combat/Dialogue） ───
        fsm.AddTransition(AudioState.Exploration, AudioTriggers.OpenMenu, AudioState.Menu);
        fsm.AddTransition(AudioState.Combat, AudioTriggers.OpenMenu, AudioState.Menu);
        fsm.AddTransition(AudioState.Dialogue, AudioTriggers.OpenMenu, AudioState.Menu);
        fsm.AddTransition(AudioState.Menu, AudioTriggers.CloseMenu, AudioState.Exploration);

        // ─── 静默 ───
        fsm.AddTransition(AudioState.Exploration, AudioTriggers.ForceSilence, AudioState.Silence);
        fsm.AddTransition(AudioState.Combat, AudioTriggers.ForceSilence, AudioState.Silence);
        fsm.AddTransition(AudioState.Dialogue, AudioTriggers.ForceSilence, AudioState.Silence);
        fsm.AddTransition(AudioState.Silence, AudioTriggers.Resume, AudioState.Exploration);

        return fsm;
    }
}
