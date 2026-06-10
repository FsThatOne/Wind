using Xunit;
using FengZhi.Foundation.NpcState;

namespace FengZhi.Tests.Foundation.NpcStates;

public class AttitudeFormulaTests
{
    // ─── AC1: Calculate 返回 clamp(-4, +3) 的分数 ──────────

    [Theory]
    [InlineData(0, 0, 0, 0, 0, AttitudeLevel.Stranger)]
    [InlineData(2, 1, 0, 0, 3, AttitudeLevel.LifeDeath)]
    [InlineData(-2, -1, -1, 0, -4, AttitudeLevel.DrawnSword)]
    [InlineData(1, 1, 0, 0, 2, AttitudeLevel.Trusted)]
    [InlineData(0, 0, 0, -1, -1, AttitudeLevel.Wary)]
    public void Calculate_ReturnsExpectedScoreAndLevel(
        int baseVal, int mindset, int morality, int misunderstanding,
        int expectedScore, AttitudeLevel expectedLevel)
    {
        var result = NpcAttitudeFormula.Calculate(baseVal, mindset, morality, misunderstanding);

        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedLevel, result.Level);
    }

    // ─── AC2: 分数正确映射为 AttitudeLevel 枚举 ────────────

    [Theory]
    [InlineData(-4, AttitudeLevel.DrawnSword)]
    [InlineData(-3, AttitudeLevel.HostileGuard)]
    [InlineData(-2, AttitudeLevel.ColdShoulder)]
    [InlineData(-1, AttitudeLevel.Wary)]
    [InlineData(0, AttitudeLevel.Stranger)]
    [InlineData(1, AttitudeLevel.Friendly)]
    [InlineData(2, AttitudeLevel.Trusted)]
    [InlineData(3, AttitudeLevel.LifeDeath)]
    public void ScoreToLevel_AllEightLevels(int score, AttitudeLevel expected)
    {
        Assert.Equal(expected, NpcAttitudeFormula.ScoreToLevel(score));
    }

    // ─── AC3: GDD 示例验证 ─────────────────────────────────

    [Fact]
    public void GddExample_Base2_Mindset1_Morality0_Misunderstanding0_Returns3_LifeDeath()
    {
        // GDD: base=+2, mindset=+1, morality=0, misunderstanding=0 → +3 → 生死相托
        var result = NpcAttitudeFormula.Calculate(2, 1, 0, 0);

        Assert.Equal(3, result.Score);
        Assert.Equal(AttitudeLevel.LifeDeath, result.Level);
        Assert.False(result.WasClamped);
    }

    // ─── AC4: 输入超出范围时钳位，不抛异常 ─────────────────

    [Fact]
    public void Calculate_Overflow_ClampsToMax()
    {
        var result = NpcAttitudeFormula.Calculate(3, 3, 3, 3);

        Assert.Equal(3, result.Score);
        Assert.Equal(AttitudeLevel.LifeDeath, result.Level);
        Assert.True(result.WasClamped);
    }

    [Fact]
    public void Calculate_Underflow_ClampsToMin()
    {
        var result = NpcAttitudeFormula.Calculate(-3, -3, -3, -3);

        Assert.Equal(-4, result.Score);
        Assert.Equal(AttitudeLevel.DrawnSword, result.Level);
        Assert.True(result.WasClamped);
    }

    // ─── AC5: mindset_mod 默认 0 时行为正确 ─────────────────

    [Fact]
    public void Calculate_MindsetModZero_UsesOnlyBaseAndOthers()
    {
        // mindset=0（NPC 未配置心境态度表时默认）
        var result = NpcAttitudeFormula.Calculate(1, 0, 1, -1);

        Assert.Equal(1, result.Score);
        Assert.Equal(AttitudeLevel.Friendly, result.Level);
    }

    // ─── 边界：精确边界值 ───────────────────────────────────

    [Fact]
    public void Calculate_ExactlyAtBoundary_NotClamped()
    {
        // 恰好等于 -4
        var result = NpcAttitudeFormula.Calculate(-2, -1, -1, 0);
        Assert.Equal(-4, result.Score);
        Assert.False(result.WasClamped);
    }

    [Fact]
    public void Calculate_ExactlyAtMaxBoundary_NotClamped()
    {
        // 恰好等于 +3
        var result = NpcAttitudeFormula.Calculate(1, 1, 1, 0);
        Assert.Equal(3, result.Score);
        Assert.False(result.WasClamped);
    }
}
