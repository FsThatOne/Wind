using System.Collections.Generic;
using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingResolutionTest
{
    private readonly MisunderstandingRegistry _registry = new(maxActivePerNpc: 5);
    private readonly MisunderstandingStateMachine _sm = new();
    private readonly MockNpcStateWriter _writer = new();
    private readonly MockConditionEvaluator _evaluator = new();
    private readonly MisunderstandingModCalculator _modCalc;
    private readonly ResolutionBonusTracker _bonusTracker;
    private readonly MisunderstandingResolver _resolver;

    public MisunderstandingResolutionTest()
    {
        var config = new MisunderstandingConfig();
        _modCalc = new MisunderstandingModCalculator(_registry, _writer);
        _bonusTracker = new ResolutionBonusTracker(_writer, config);
        _resolver = new MisunderstandingResolver(_registry, _sm, _modCalc, _evaluator, _bonusTracker);
    }

    private MisunderstandingInstance Register(string id, string npc, Severity sev, int window,
        List<string>? conditions = null, string? unlockFlag = null)
    {
        var inst = new MisunderstandingInstance
        {
            Id = id,
            TargetNpc = npc,
            SourceType = SourceType.JianghuEvent,
            Severity = sev,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = 0,
            CreatedDay = 1,
            ResolutionConditions = conditions ?? new List<string> { "cond_a" },
            UnlockFlag = unlockFlag,
        };
        inst.State = MisunderstandingState.Active;
        _registry.TryRegister(inst);
        return inst;
    }

    [Fact]
    public void ConditionsMet_ActiveModerate_ResolvedAndModZero()
    {
        var inst = Register("a", "npc1", Severity.Moderate, 5);
        _evaluator.SetResult(true);

        var resolved = _resolver.TryResolveAll("npc1");

        Assert.Single(resolved);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
        Assert.Equal(0, _writer.LastMod("npc1"));
    }

    [Fact]
    public void BonusApplied_ExpiresAfter3Days()
    {
        Register("a", "npc1", Severity.Minor, 5);
        _evaluator.SetResult(true);
        _resolver.TryResolveAll("npc1");

        Assert.True(_bonusTracker.HasBonus("npc1"));
        Assert.Equal(1, _writer.LastBonusValue("npc1"));
        Assert.Equal(3, _writer.LastBonusDuration("npc1"));

        // tick 3 天 → bonus 过期
        _bonusTracker.OnDayAdvanced(new[] { "npc1" });
        _bonusTracker.OnDayAdvanced(new[] { "npc1" });
        _bonusTracker.OnDayAdvanced(new[] { "npc1" });

        Assert.False(_bonusTracker.HasBonus("npc1"));
        Assert.Equal(0, _writer.LastBonusValue("npc1"));
    }

    [Fact]
    public void SameDayResolveAndEscalate_ResolveTakesPriority()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        _evaluator.SetResult(true);

        // 澄清优先执行
        var resolved = _resolver.TryResolveAll("npc1");
        Assert.Single(resolved);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
        Assert.Equal(Severity.Minor, inst.Severity);
    }

    [Fact]
    public void PermanentWithUnlockFlag_CanResolve()
    {
        var inst = Register("a", "npc1", Severity.Minor, 0,
            conditions: new List<string> { "cond_x" },
            unlockFlag: "unlock_permanent_mis");
        inst.State = MisunderstandingState.Permanent;
        _evaluator.SetResult(true);

        _resolver.RegisterPermanentUnlock("unlock_permanent_mis");
        var resolved = _resolver.TryResolveAll("npc1");

        Assert.Single(resolved);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
    }

    [Fact]
    public void HiddenTransparency_ConditionsMet_StillResolves()
    {
        var inst = Register("a", "npc1", Severity.Minor, 5);
        inst.Transparency = Transparency.Hidden;
        _evaluator.SetResult(true);

        var resolved = _resolver.TryResolveAll("npc1");

        Assert.Single(resolved);
        Assert.Equal(MisunderstandingState.Resolved, inst.State);
    }

    [Fact]
    public void PartialConditions_DoesNotResolve()
    {
        var inst = Register("a", "npc1", Severity.Moderate, 5);
        _evaluator.SetResult(false);

        var resolved = _resolver.TryResolveAll("npc1");

        Assert.Empty(resolved);
        Assert.Equal(MisunderstandingState.Active, inst.State);
    }

    [Fact]
    public void BonusResetTimerOnSecondResolve_NoStackValue()
    {
        Register("a", "npc1", Severity.Minor, 5, conditions: new List<string> { "c1" });
        Register("b", "npc1", Severity.Minor, 5, conditions: new List<string> { "c2" });
        _evaluator.SetResult(true);

        // 解除第一条
        _resolver.TryResolveSingle("npc1", "a");
        Assert.Equal(3, _bonusTracker.GetRemainingDays("npc1"));

        // tick 2 天
        _bonusTracker.OnDayAdvanced(new[] { "npc1" });
        _bonusTracker.OnDayAdvanced(new[] { "npc1" });
        Assert.Equal(1, _bonusTracker.GetRemainingDays("npc1"));

        // 解除第二条 → 重置计时为 3 天，数值仍为 +1（不叠加）
        _resolver.TryResolveSingle("npc1", "b");
        Assert.Equal(3, _bonusTracker.GetRemainingDays("npc1"));
        Assert.Equal(1, _writer.LastBonusValue("npc1"));
    }

    private sealed class MockNpcStateWriter : INpcStateWriter
    {
        private readonly Dictionary<string, int> _mods = new();
        private readonly Dictionary<string, (int value, int duration)> _bonuses = new();

        public void SetMisunderstandingMod(string npcId, int value) => _mods[npcId] = value;

        public void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays)
            => _bonuses[npcId] = (bonusValue, durationDays);

        public int LastMod(string npcId) => _mods.GetValueOrDefault(npcId);
        public int LastBonusValue(string npcId) => _bonuses.TryGetValue(npcId, out var b) ? b.value : 0;
        public int LastBonusDuration(string npcId) => _bonuses.TryGetValue(npcId, out var b) ? b.duration : 0;
    }

    private sealed class MockConditionEvaluator : IConditionEvaluator
    {
        private bool _result;
        public void SetResult(bool val) => _result = val;
        public bool AreConditionsMet(List<string> conditionIds) => _result;
    }
}
