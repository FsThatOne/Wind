using Xunit;
using FengZhi.Foundation.TimeSystem;

namespace FengZhi.Tests.Foundation.TimeSystem;

public class StaminaSystemTests
{
    // ─── AC1: F1 上限计算 ───────────────────────────────────

    [Fact]
    public void MaxStamina_Formula_BaseAndConstitution()
    {
        // base=100, con=10, per_con=5 → 100 + 10*5 = 150
        var sys = new StaminaSystem(constitution: 10);
        Assert.Equal(150f, sys.MaxStamina);
    }

    [Fact]
    public void MaxStamina_CustomParams()
    {
        var sys = new StaminaSystem(constitution: 20, baseStamina: 50, staminaPerCon: 3);
        Assert.Equal(110f, sys.MaxStamina); // 50 + 20*3 = 110
    }

    [Fact]
    public void InitialStamina_EqualsMax()
    {
        var sys = new StaminaSystem(constitution: 10);
        Assert.Equal(sys.MaxStamina, sys.CurrentStamina);
    }

    // ─── AC2: 消耗体力，力竭后不扣 ─────────────────────────

    [Fact]
    public void ConsumeStamina_Normal_DeductsAmount()
    {
        var sys = new StaminaSystem(constitution: 10);
        float consumed = sys.ConsumeStamina(50f);
        Assert.Equal(50f, consumed);
        Assert.Equal(100f, sys.CurrentStamina);
    }

    [Fact]
    public void ConsumeStamina_MoreThanCurrent_ClampsToZero()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(140f); // 还剩 10
        float consumed = sys.ConsumeStamina(20f); // 只能扣 10
        Assert.Equal(10f, consumed);
        Assert.Equal(0f, sys.CurrentStamina);
    }

    [Fact]
    public void ConsumeStamina_WhenExhausted_ReturnsZero()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(150f); // 力竭
        float consumed = sys.ConsumeStamina(10f);
        Assert.Equal(0f, consumed);
        Assert.Equal(0f, sys.CurrentStamina);
    }

    [Fact]
    public void ConsumeStamina_NegativeAmount_ReturnsZero()
    {
        var sys = new StaminaSystem(constitution: 10);
        float consumed = sys.ConsumeStamina(-5f);
        Assert.Equal(0f, consumed);
        Assert.Equal(sys.MaxStamina, sys.CurrentStamina);
    }

    // ─── AC3: 恢复体力，不超上限 ────────────────────────────

    [Fact]
    public void RestoreStamina_Normal_AddsAmount()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(100f); // 剩50
        float restored = sys.RestoreStamina(30f);
        Assert.Equal(30f, restored);
        Assert.Equal(80f, sys.CurrentStamina);
    }

    [Fact]
    public void RestoreStamina_ExceedsMax_ClampsToMax()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(20f); // 剩 130
        float restored = sys.RestoreStamina(50f); // 只需 20 填满
        Assert.Equal(20f, restored);
        Assert.Equal(150f, sys.CurrentStamina);
    }

    [Fact]
    public void RestoreStamina_NegativeAmount_ReturnsZero()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(50f);
        float restored = sys.RestoreStamina(-10f);
        Assert.Equal(0f, restored);
    }

    // ─── AC4: F5 状态判定 ────────────────────────────────────

    [Fact]
    public void CurrentState_Vigorous_WhenAbove50Percent()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150
        sys.ConsumeStamina(74f); // 剩 76 → 76/150=50.67% > 50%
        Assert.Equal(StaminaState.Vigorous, sys.CurrentState);
    }

    [Fact]
    public void CurrentState_Fatigued_WhenAtOrBelow50Percent()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150
        sys.ConsumeStamina(75f); // 剩 75 → 75/150=50% 恰好 ≤ 50%
        Assert.Equal(StaminaState.Fatigued, sys.CurrentState);
    }

    [Fact]
    public void CurrentState_Exhausted_WhenZero()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(150f);
        Assert.Equal(StaminaState.Exhausted, sys.CurrentState);
    }

    // ─── AC5: F3 休息恢复 ────────────────────────────────────

    [Fact]
    public void RestByRatio_Restores30Percent()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150
        sys.ConsumeStamina(150f); // 力竭
        float restored = sys.RestByRatio(0.3f); // 30% of 150 = 45
        Assert.Equal(45f, restored);
        Assert.Equal(45f, sys.CurrentStamina);
    }

    [Fact]
    public void RestByRatio_DoesNotExceedMax()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150
        sys.ConsumeStamina(10f); // 剩 140，缺口仅 10
        float restored = sys.RestByRatio(0.3f); // 想恢复 45 但缺口只有 10
        Assert.Equal(10f, restored);
        Assert.Equal(150f, sys.CurrentStamina);
    }

    [Fact]
    public void RestFull_RestoresToMax()
    {
        var sys = new StaminaSystem(constitution: 10);
        sys.ConsumeStamina(100f); // 剩 50
        float restored = sys.RestFull();
        Assert.Equal(100f, restored);
        Assert.Equal(150f, sys.CurrentStamina);
    }

    // ─── AC6: 体魄变化重算上限 ──────────────────────────────

    [Fact]
    public void UpdateConstitution_Increase_DoesNotChangeCurrentStamina()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150, cur=150
        sys.ConsumeStamina(50f); // cur=100
        sys.UpdateConstitution(15); // new max = 100+15*5 = 175
        Assert.Equal(175f, sys.MaxStamina);
        Assert.Equal(100f, sys.CurrentStamina); // 不变
    }

    [Fact]
    public void UpdateConstitution_Decrease_ClampsIfExceedsNewMax()
    {
        var sys = new StaminaSystem(constitution: 10); // max=150, cur=150
        sys.UpdateConstitution(5); // new max = 100+5*5 = 125
        Assert.Equal(125f, sys.MaxStamina);
        Assert.Equal(125f, sys.CurrentStamina); // clamp to new max
    }
}
