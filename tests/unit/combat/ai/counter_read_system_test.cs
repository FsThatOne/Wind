using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat.AI;
using Xunit;

namespace Foundation.Tests.Combat.AI;

/// <summary>
/// ai-005: 反读系统 (Counter-Read Phase B) 验证。
/// GDD AC#4: Boss 反读概率 40%，1000 次在 370-430 范围内。
/// GDD AC#20: 多人战反读基于连续同体系最高者。
/// GDD AC#21: 冷却减半后概率 = 20%，1000 次在 175-225 范围内。
/// </summary>
public class CounterReadSystemTests
{
    // --- GetConsecutiveCount ---

    [Fact]
    public void GetConsecutiveCount_EmptyHistory_ReturnsZero()
    {
        Assert.Equal(0, CounterReadSystem.GetConsecutiveCount(Array.Empty<MoveType>()));
    }

    [Fact]
    public void GetConsecutiveCount_SingleEntry_ReturnsOne()
    {
        Assert.Equal(1, CounterReadSystem.GetConsecutiveCount(new[] { MoveType.Gang }));
    }

    [Fact]
    public void GetConsecutiveCount_AllSame_ReturnsCount()
    {
        var history = new[] { MoveType.Rou, MoveType.Rou, MoveType.Rou };
        Assert.Equal(3, CounterReadSystem.GetConsecutiveCount(history));
    }

    [Fact]
    public void GetConsecutiveCount_MixedThenConsecutive_ReturnsTrailingCount()
    {
        var history = new[] { MoveType.Gang, MoveType.Qiao, MoveType.Qiao };
        Assert.Equal(2, CounterReadSystem.GetConsecutiveCount(history));
    }

    [Fact]
    public void GetConsecutiveCount_NoConsecutive_ReturnsOne()
    {
        var history = new[] { MoveType.Gang, MoveType.Rou, MoveType.Qiao };
        Assert.Equal(1, CounterReadSystem.GetConsecutiveCount(history));
    }

    // --- GetCounterType ---

    [Theory]
    [InlineData(MoveType.Gang, MoveType.Rou)]   // 柔克刚
    [InlineData(MoveType.Qiao, MoveType.Gang)]  // 刚克巧
    [InlineData(MoveType.Rou, MoveType.Qiao)]   // 巧克柔
    public void GetCounterType_ReturnsCorrectCounter(MoveType target, MoveType expected)
    {
        Assert.Equal(expected, CounterReadSystem.GetCounterType(target));
    }

    // --- TryCounterRead ---

    [Fact]
    public void TryCounterRead_ChanceZero_NeverTriggers()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Rou);
        system.RecordPlayerAction("p1", MoveType.Rou);
        var random = new FixedAIRandom(0.0f);
        Assert.Null(system.TryCounterRead(0f, random));
    }

    [Fact]
    public void TryCounterRead_NoConsecutive_NeverTriggers()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.RecordPlayerAction("p1", MoveType.Rou); // 不同体系
        var random = new FixedAIRandom(0.0f);
        Assert.Null(system.TryCounterRead(0.4f, random));
    }

    [Fact]
    public void TryCounterRead_Consecutive2_LowRoll_TriggersCounter()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Rou);
        system.RecordPlayerAction("p1", MoveType.Rou);
        var random = new FixedAIRandom(0.1f); // < 0.4
        var result = system.TryCounterRead(0.4f, random);
        Assert.Equal(MoveType.Qiao, result); // 巧克柔
    }

    [Fact]
    public void TryCounterRead_Consecutive2_HighRoll_DoesNotTrigger()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Rou);
        system.RecordPlayerAction("p1", MoveType.Rou);
        var random = new FixedAIRandom(0.5f); // > 0.4
        Assert.Null(system.TryCounterRead(0.4f, random));
    }

    [Fact]
    public void TryCounterRead_WithCooldown_HalvesChance()
    {
        // GDD AC#21: 上回合已触发 → 概率减半
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.RecordPlayerAction("p1", MoveType.Gang);

        // 先触发一次
        var random1 = new FixedAIRandom(0.0f);
        var result1 = system.TryCounterRead(0.4f, random1);
        Assert.NotNull(result1); // 触发了
        Assert.True(system.LastRoundTriggered);

        // 记录新回合，继续连续
        system.RecordPlayerAction("p1", MoveType.Gang);

        // 冷却生效：0.4 × (1 - 0.5) = 0.2
        var random2 = new FixedAIRandom(0.19f); // < 0.2
        var result2 = system.TryCounterRead(0.4f, random2);
        Assert.NotNull(result2);

        // 冷却生效：roll >= 0.2 不触发
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.EndRound(triggered: false); // 模拟上回合未触发
        // 无冷却 → 回到 0.4
        var random3 = new FixedAIRandom(0.39f); // < 0.4
        var result3 = system.TryCounterRead(0.4f, random3);
        Assert.NotNull(result3);
    }

    // --- GDD AC#4: 1000 次统计验证 ---

    [Fact]
    public void TryCounterRead_AC4_1000Trials_40Percent_InRange370To430()
    {
        int triggerCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var system = new CounterReadSystem();
            system.RecordPlayerAction("p1", MoveType.Rou);
            system.RecordPlayerAction("p1", MoveType.Rou);
            var random = new SeededAIRandom(i);
            if (system.TryCounterRead(0.4f, random) != null)
                triggerCount++;
        }
        Assert.InRange(triggerCount, 370, 430);
    }

    // --- GDD AC#21: 冷却后 20% 概率统计验证 ---

    [Fact]
    public void TryCounterRead_AC21_1000Trials_CooldownHalf_InRange175To225()
    {
        int triggerCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var system = new CounterReadSystem();
            system.RecordPlayerAction("p1", MoveType.Rou);
            system.RecordPlayerAction("p1", MoveType.Rou);
            // 模拟上回合已触发
            system.EndRound(triggered: true);
            var random = new SeededAIRandom(i);
            if (system.TryCounterRead(0.4f, random) != null)
                triggerCount++;
        }
        Assert.InRange(triggerCount, 175, 225);
    }

    // --- GDD AC#20: 多人战反读 ---

    [Fact]
    public void TryCounterRead_MultiPlayer_PicksHighestConsecutive()
    {
        var system = new CounterReadSystem();
        // 角色A: 连续2回合柔
        system.RecordPlayerAction("A", MoveType.Rou);
        system.RecordPlayerAction("A", MoveType.Rou);
        // 角色B: 混合体系
        system.RecordPlayerAction("B", MoveType.Gang);
        system.RecordPlayerAction("B", MoveType.Qiao);

        var random = new FixedAIRandom(0.0f);
        var result = system.TryCounterRead(0.4f, random);
        // 应该基于角色A的数据：克柔→巧
        Assert.Equal(MoveType.Qiao, result);
    }

    [Fact]
    public void TryCounterRead_MultiPlayer_NeitherConsecutive_NoTrigger()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("A", MoveType.Gang);
        system.RecordPlayerAction("A", MoveType.Rou);
        system.RecordPlayerAction("B", MoveType.Qiao);
        system.RecordPlayerAction("B", MoveType.Gang);

        var random = new FixedAIRandom(0.0f);
        Assert.Null(system.TryCounterRead(0.4f, random));
    }

    // --- 历史窗口限制 ---

    [Fact]
    public void RecordPlayerAction_ExceedsWindow_TrimsOldest()
    {
        var system = new CounterReadSystem(historyWindow: 3);
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.RecordPlayerAction("p1", MoveType.Rou); // 超出窗口

        var histories = system.GetPlayerHistories();
        Assert.Equal(3, histories["p1"].Count); // 窗口3
        // 最新3条：Gang, Gang, Rou → 连续只有1
        Assert.Equal(1, CounterReadSystem.GetConsecutiveCount(histories["p1"]));
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var system = new CounterReadSystem();
        system.RecordPlayerAction("p1", MoveType.Gang);
        system.EndRound(triggered: true);
        system.Reset();

        Assert.Empty(system.GetPlayerHistories());
        Assert.False(system.LastRoundTriggered);
    }
}
