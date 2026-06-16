using Xunit;
using FengZhi.Foundation.StateMachine;

namespace FengZhi.Tests.Foundation.StateMachine;

public enum TestState { Idle, Walking, Running, Combat, Dead }

public class GenericFsmTests
{
    // ─── AC1: 注册状态和转换规则 ─────────────────────────────

    [Fact]
    public void RegisterState_AddsToRegisteredStates()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.RegisterState(TestState.Walking);
        fsm.RegisterState(TestState.Running);

        Assert.Contains(TestState.Idle, fsm.RegisteredStates);
        Assert.Contains(TestState.Walking, fsm.RegisteredStates);
        Assert.Contains(TestState.Running, fsm.RegisteredStates);
    }

    [Fact]
    public void AddTransition_AutoRegistersStates()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);

        Assert.Contains(TestState.Walking, fsm.RegisteredStates);
    }

    // ─── AC2: Guard 条件检查 ────────────────────────────────

    [Fact]
    public void TryTransition_GuardReturnsFalse_TransitionRejected()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "combat", TestState.Combat, guard: () => false);

        bool result = fsm.TryTransition("combat");

        Assert.False(result);
        Assert.Equal(TestState.Idle, fsm.CurrentState);
    }

    [Fact]
    public void TryTransition_GuardReturnsTrue_TransitionSucceeds()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "combat", TestState.Combat, guard: () => true);

        bool result = fsm.TryTransition("combat");

        Assert.True(result);
        Assert.Equal(TestState.Combat, fsm.CurrentState);
    }

    [Fact]
    public void TryTransition_NoGuard_TransitionSucceeds()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);

        Assert.True(fsm.TryTransition("walk"));
        Assert.Equal(TestState.Walking, fsm.CurrentState);
    }

    // ─── AC3: OnExit → OnEnter 回调 ─────────────────────────

    [Fact]
    public void TryTransition_FiresOnExitThenOnEnter()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);

        var callOrder = new List<string>();
        fsm.OnStateExit += (from, to) => callOrder.Add($"exit:{from}->{to}");
        fsm.OnStateEnter += (from, to) => callOrder.Add($"enter:{from}->{to}");

        fsm.TryTransition("walk");

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("exit:Idle->Walking", callOrder[0]);
        Assert.Equal("enter:Idle->Walking", callOrder[1]);
    }

    [Fact]
    public void TryTransition_GuardFails_NoCallbacksFired()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking, () => false);

        bool exitFired = false;
        bool enterFired = false;
        fsm.OnStateExit += (_, _) => exitFired = true;
        fsm.OnStateEnter += (_, _) => enterFired = true;

        fsm.TryTransition("walk");

        Assert.False(exitFired);
        Assert.False(enterFired);
    }

    // ─── AC4: 未注册转换返回 false ──────────────────────────

    [Fact]
    public void TryTransition_UnregisteredTrigger_ReturnsFalse()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);

        Assert.False(fsm.TryTransition("fly"));
        Assert.Equal(TestState.Idle, fsm.CurrentState);
    }

    [Fact]
    public void TryTransition_WrongSourceState_ReturnsFalse()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Walking, "run", TestState.Running);

        Assert.False(fsm.TryTransition("run"));
        Assert.Equal(TestState.Idle, fsm.CurrentState);
    }

    // ─── AC5: ForceTransition 跳过 guard ────────────────────

    [Fact]
    public void ForceTransition_SkipsGuard()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "combat", TestState.Combat, guard: () => false);

        bool result = fsm.ForceTransition(TestState.Combat, "main_quest_override");

        Assert.True(result);
        Assert.Equal(TestState.Combat, fsm.CurrentState);
    }

    [Fact]
    public void ForceTransition_UnregisteredState_ReturnsFalse()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        // Dead 未注册
        Assert.False(fsm.ForceTransition(TestState.Dead));
        Assert.Equal(TestState.Idle, fsm.CurrentState);
    }

    [Fact]
    public void ForceTransition_RegisteredState_FiresCallbacks()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.RegisterState(TestState.Dead);

        var callOrder = new List<string>();
        fsm.OnStateExit += (from, to) => callOrder.Add($"exit:{from}");
        fsm.OnStateEnter += (from, to) => callOrder.Add($"enter:{to}");

        fsm.ForceTransition(TestState.Dead, "killed");

        Assert.Equal("exit:Idle", callOrder[0]);
        Assert.Equal("enter:Dead", callOrder[1]);
    }

    // ─── AC6: CurrentState 和 History ───────────────────────

    [Fact]
    public void CurrentState_ReflectsLatestTransition()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);
        fsm.AddTransition(TestState.Walking, "run", TestState.Running);

        fsm.TryTransition("walk");
        fsm.TryTransition("run");

        Assert.Equal(TestState.Running, fsm.CurrentState);
    }

    [Fact]
    public void History_RecordsTransitions()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);
        fsm.AddTransition(TestState.Walking, "run", TestState.Running);

        fsm.TryTransition("walk");
        fsm.TryTransition("run");

        Assert.Equal(2, fsm.History.Count);
        Assert.Equal(TestState.Idle, fsm.History[0].From);
        Assert.Equal(TestState.Walking, fsm.History[0].To);
        Assert.Equal("walk", fsm.History[0].Trigger);
        Assert.Equal(TestState.Walking, fsm.History[1].From);
        Assert.Equal(TestState.Running, fsm.History[1].To);
    }

    [Fact]
    public void History_TruncatesToMaxLength()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle) { MaxHistoryLength = 3 };
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);
        fsm.AddTransition(TestState.Walking, "idle", TestState.Idle);

        for (int i = 0; i < 5; i++)
        {
            fsm.TryTransition("walk");
            fsm.TryTransition("idle");
        }

        Assert.Equal(3, fsm.History.Count);
    }

    // ─── CanTransition 辅助查询 ─────────────────────────────

    [Fact]
    public void CanTransition_ValidTriggerNoGuard_ReturnsTrue()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking);

        Assert.True(fsm.CanTransition("walk"));
    }

    [Fact]
    public void CanTransition_GuardFails_ReturnsFalse()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);
        fsm.AddTransition(TestState.Idle, "walk", TestState.Walking, () => false);

        Assert.False(fsm.CanTransition("walk"));
    }

    [Fact]
    public void CanTransition_NoRule_ReturnsFalse()
    {
        var fsm = new StateMachine<TestState>(TestState.Idle);

        Assert.False(fsm.CanTransition("fly"));
    }
}
