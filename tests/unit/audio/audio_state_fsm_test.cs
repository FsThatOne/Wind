using Xunit;
using FengZhi.Foundation.Audio;
using FengZhi.Foundation.StateMachine;

namespace FengZhi.Tests.Unit.Audio;

public class AudioStateFsmTest
{
    private readonly StateMachine<AudioState> _fsm;

    public AudioStateFsmTest()
    {
        _fsm = AudioStateMachineFactory.Create();
    }

    // ─── AC: AudioState 6 状态正确注册 ─────────────────────────

    [Fact]
    public void AllSixStatesRegistered()
    {
        Assert.Contains(AudioState.Exploration, _fsm.RegisteredStates);
        Assert.Contains(AudioState.Combat, _fsm.RegisteredStates);
        Assert.Contains(AudioState.Cutscene, _fsm.RegisteredStates);
        Assert.Contains(AudioState.Dialogue, _fsm.RegisteredStates);
        Assert.Contains(AudioState.Menu, _fsm.RegisteredStates);
        Assert.Contains(AudioState.Silence, _fsm.RegisteredStates);
        Assert.Equal(6, _fsm.RegisteredStates.Count);
    }

    [Fact]
    public void InitialStateIsExploration()
    {
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    // ─── AC: 状态转换规则完整配置 ──────────────────────────────

    [Fact]
    public void ExplorationToCombat_EnterCombat()
    {
        Assert.True(_fsm.TryTransition(AudioTriggers.EnterCombat));
        Assert.Equal(AudioState.Combat, _fsm.CurrentState);
    }

    [Fact]
    public void CombatToExploration_ExitCombat()
    {
        _fsm.TryTransition(AudioTriggers.EnterCombat);
        Assert.True(_fsm.TryTransition(AudioTriggers.ExitCombat));
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    [Fact]
    public void ExplorationToCutscene_StartCutscene()
    {
        Assert.True(_fsm.TryTransition(AudioTriggers.StartCutscene));
        Assert.Equal(AudioState.Cutscene, _fsm.CurrentState);
    }

    [Fact]
    public void CombatToCutscene_StartCutscene()
    {
        _fsm.TryTransition(AudioTriggers.EnterCombat);
        Assert.True(_fsm.TryTransition(AudioTriggers.StartCutscene));
        Assert.Equal(AudioState.Cutscene, _fsm.CurrentState);
    }

    [Fact]
    public void ExplorationToDialogue_StartDialogue()
    {
        Assert.True(_fsm.TryTransition(AudioTriggers.StartDialogue));
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);
    }

    [Fact]
    public void CombatToDialogue_StartDialogue()
    {
        _fsm.TryTransition(AudioTriggers.EnterCombat);
        Assert.True(_fsm.TryTransition(AudioTriggers.StartDialogue));
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);
    }

    [Fact]
    public void ExplorationToMenu_OpenMenu()
    {
        Assert.True(_fsm.TryTransition(AudioTriggers.OpenMenu));
        Assert.Equal(AudioState.Menu, _fsm.CurrentState);
    }

    [Fact]
    public void DialogueToMenu_OpenMenu()
    {
        _fsm.TryTransition(AudioTriggers.StartDialogue);
        Assert.True(_fsm.TryTransition(AudioTriggers.OpenMenu));
        Assert.Equal(AudioState.Menu, _fsm.CurrentState);
    }

    [Fact]
    public void ExplorationToSilence_ForceSilence()
    {
        Assert.True(_fsm.TryTransition(AudioTriggers.ForceSilence));
        Assert.Equal(AudioState.Silence, _fsm.CurrentState);
    }

    [Fact]
    public void SilenceToExploration_Resume()
    {
        _fsm.TryTransition(AudioTriggers.ForceSilence);
        Assert.True(_fsm.TryTransition(AudioTriggers.Resume));
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    [Fact]
    public void InvalidTransition_ReturnsFalse()
    {
        // Can't go directly from Exploration to Menu-close
        Assert.False(_fsm.TryTransition(AudioTriggers.CloseMenu));
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    // ─── AC5: Master=0 时状态机正常运转 ─────────────────────────

    [Fact]
    public void AC5_FsmRunsIndependentlyOfVolume_TransitionSequence()
    {
        // 模拟 Master=0 时的状态切换序列
        // EnterCombat → ExitCombat → StartDialogue → EndDialogue
        Assert.True(_fsm.TryTransition(AudioTriggers.EnterCombat));
        Assert.Equal(AudioState.Combat, _fsm.CurrentState);

        Assert.True(_fsm.TryTransition(AudioTriggers.ExitCombat));
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);

        Assert.True(_fsm.TryTransition(AudioTriggers.StartDialogue));
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);

        Assert.True(_fsm.TryTransition(AudioTriggers.EndDialogue));
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    [Fact]
    public void AC5_AttenuationAfterRestore_MatchesCurrentState()
    {
        // Master=0 期间走过 Combat → Exploration，恢复音量后 attenuation 应为 Exploration 值
        _fsm.TryTransition(AudioTriggers.EnterCombat);
        _fsm.TryTransition(AudioTriggers.ExitCombat);

        var (bgm, ambient, sfx) = AudioStateAttenuation.GetAll(_fsm.CurrentState);
        Assert.Equal(1.0f, bgm);
        Assert.Equal(1.0f, ambient);
        Assert.Equal(1.0f, sfx);
    }

    // ─── AC6: 对话衰减 ─────────────────────────────────────────

    [Fact]
    public void AC6_DialogueState_BgmAttenuatedTo60Percent()
    {
        _fsm.TryTransition(AudioTriggers.StartDialogue);

        float bgm = AudioStateAttenuation.GetAttenuation(AudioState.Dialogue, AudioTrack.Bgm);
        Assert.Equal(0.6f, bgm);
    }

    [Fact]
    public void AC6_DialogueState_AmbientAttenuatedTo40Percent()
    {
        _fsm.TryTransition(AudioTriggers.StartDialogue);

        float ambient = AudioStateAttenuation.GetAttenuation(AudioState.Dialogue, AudioTrack.Ambient);
        Assert.Equal(0.4f, ambient);
    }

    [Fact]
    public void AC6_DialogueEnd_RestoresExplorationAttenuation()
    {
        _fsm.TryTransition(AudioTriggers.StartDialogue);
        _fsm.TryTransition(AudioTriggers.EndDialogue);

        var (bgm, ambient, sfx) = AudioStateAttenuation.GetAll(_fsm.CurrentState);
        Assert.Equal(1.0f, bgm);
        Assert.Equal(1.0f, ambient);
        Assert.Equal(1.0f, sfx);
    }

    // ─── AC: state_attenuation 矩阵完整性 ──────────────────────

    [Theory]
    [InlineData(AudioState.Exploration, 1.0f, 1.0f, 1.0f)]
    [InlineData(AudioState.Combat, 1.0f, 0.2f, 1.0f)]
    [InlineData(AudioState.Cutscene, 1.0f, 0.0f, 1.0f)]
    [InlineData(AudioState.Dialogue, 0.6f, 0.4f, 1.0f)]
    [InlineData(AudioState.Menu, 0.4f, 0.3f, 1.0f)]
    [InlineData(AudioState.Silence, 0.0f, 1.0f, 1.0f)]
    public void AttenuationMatrix_MatchesGddF2(AudioState state, float expectedBgm, float expectedAmbient, float expectedSfx)
    {
        var (bgm, ambient, sfx) = AudioStateAttenuation.GetAll(state);
        Assert.Equal(expectedBgm, bgm);
        Assert.Equal(expectedAmbient, ambient);
        Assert.Equal(expectedSfx, sfx);
    }

    // ─── OnStateEnter/Exit 回调验证 attenuation 变更时机 ────────

    [Fact]
    public void StateEnterCallback_FiredOnTransition()
    {
        AudioState? enteredState = null;
        _fsm.OnStateEnter += (from, to) => enteredState = to;

        _fsm.TryTransition(AudioTriggers.EnterCombat);

        Assert.Equal(AudioState.Combat, enteredState);
    }

    [Fact]
    public void StateExitCallback_FiredBeforeEnter()
    {
        var order = new List<string>();
        _fsm.OnStateExit += (from, to) => order.Add($"exit:{from}");
        _fsm.OnStateEnter += (from, to) => order.Add($"enter:{to}");

        _fsm.TryTransition(AudioTriggers.EnterCombat);

        Assert.Equal("exit:Exploration", order[0]);
        Assert.Equal("enter:Combat", order[1]);
    }

    // ─── 边缘情况：演出中无法进入对话 ──────────────────────────

    [Fact]
    public void CutsceneState_CannotStartDialogue()
    {
        _fsm.TryTransition(AudioTriggers.StartCutscene);
        Assert.False(_fsm.TryTransition(AudioTriggers.StartDialogue));
        Assert.Equal(AudioState.Cutscene, _fsm.CurrentState);
    }

    [Fact]
    public void SilenceState_CannotEnterCombat()
    {
        _fsm.TryTransition(AudioTriggers.ForceSilence);
        Assert.False(_fsm.TryTransition(AudioTriggers.EnterCombat));
        Assert.Equal(AudioState.Silence, _fsm.CurrentState);
    }

    // ─── 嵌套状态返回正确性（AudioDirector _returnStack 逻辑） ──

    [Fact]
    public void NestedReturn_DialogueThenMenu_ReturnsCorrectly()
    {
        var returnStack = new Stack<AudioState>();

        // Exploration → Dialogue (push Exploration)
        returnStack.Push(_fsm.CurrentState);
        _fsm.TryTransition(AudioTriggers.StartDialogue);
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);

        // Dialogue → Menu (push Dialogue)
        returnStack.Push(_fsm.CurrentState);
        _fsm.TryTransition(AudioTriggers.OpenMenu);
        Assert.Equal(AudioState.Menu, _fsm.CurrentState);

        // CloseMenu → pop Dialogue
        var menuReturn = returnStack.Pop();
        _fsm.ForceTransition(menuReturn, AudioTriggers.CloseMenu);
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);

        // EndDialogue → pop Exploration
        var dialogueReturn = returnStack.Pop();
        _fsm.ForceTransition(dialogueReturn, AudioTriggers.EndDialogue);
        Assert.Equal(AudioState.Exploration, _fsm.CurrentState);
    }

    [Fact]
    public void NestedReturn_CombatDialogueCutscene_ReturnsCorrectly()
    {
        var returnStack = new Stack<AudioState>();

        // Exploration → Combat (direct, clear stack)
        returnStack.Clear();
        _fsm.TryTransition(AudioTriggers.EnterCombat);

        // Combat → Dialogue (push Combat)
        returnStack.Push(_fsm.CurrentState);
        _fsm.TryTransition(AudioTriggers.StartDialogue);

        // Dialogue → Cutscene (push Dialogue)
        returnStack.Push(_fsm.CurrentState);
        _fsm.TryTransition(AudioTriggers.StartCutscene);
        Assert.Equal(AudioState.Cutscene, _fsm.CurrentState);

        // EndCutscene → pop Dialogue
        _fsm.ForceTransition(returnStack.Pop(), AudioTriggers.EndCutscene);
        Assert.Equal(AudioState.Dialogue, _fsm.CurrentState);

        // EndDialogue → pop Combat
        _fsm.ForceTransition(returnStack.Pop(), AudioTriggers.EndDialogue);
        Assert.Equal(AudioState.Combat, _fsm.CurrentState);
    }
}
