using Xunit;
using FengZhi.Foundation.CharacterData;

namespace FengZhi.Tests.Foundation.CharacterData;

public class ModifierStackTests
{
    // ─── AC1: 同源不叠加，取最高值 ─────────────────────────

    [Fact]
    public void SameSource_SameAttribute_KeepsHighestValue()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "equipment:iron_sword",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Attack,
            Value = 5
        });

        stack.Add(new AttributeModifier
        {
            Source = "equipment:iron_sword",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Attack,
            Value = 8
        });

        // 同源只取最高值 8
        Assert.Equal(8, stack.GetSum(AttributeType.Attack));
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void SameSource_LowerValue_Ignored()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "buff:tiger",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Strength,
            Value = 10
        });

        stack.Add(new AttributeModifier
        {
            Source = "buff:tiger",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Strength,
            Value = 6
        });

        // 第二次较低，被忽略
        Assert.Equal(10, stack.GetSum(AttributeType.Strength));
        Assert.Equal(1, stack.Count);
    }

    // ─── AC2: 不同来源正常叠加 ──────────────────────────────

    [Fact]
    public void DifferentSources_StackNormally()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "equipment:iron_sword",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Attack,
            Value = 5
        });

        stack.Add(new AttributeModifier
        {
            Source = "buff:berserk",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Attack,
            Value = 3
        });

        Assert.Equal(8, stack.GetSum(AttributeType.Attack));
        Assert.Equal(2, stack.Count);
    }

    // ─── AC3: Temporary duration 递减+自动移除 ──────────────

    [Fact]
    public void TickTurn_DecrementsTemporaryDuration()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "buff:speed",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Speed,
            Value = 3,
            Duration = 2
        });

        stack.TickTurn(); // 2→1
        Assert.Equal(3, stack.GetSum(AttributeType.Speed));
        Assert.Equal(1, stack.Count);

        stack.TickTurn(); // 1→0 → 移除
        Assert.Equal(0, stack.GetSum(AttributeType.Speed));
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void TickTurn_DoesNotAffectPermanent()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "growth:ch1_node1",
            Layer = ModifierLayer.Permanent,
            Attribute = AttributeType.Strength,
            Value = 5,
            Duration = -1
        });

        stack.TickTurn();
        stack.TickTurn();
        stack.TickTurn();

        Assert.Equal(5, stack.GetSum(AttributeType.Strength));
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void TickTurn_InfiniteDuration_NotRemoved()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "buff:aura",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Defense,
            Value = 2,
            Duration = -1 // 无限持续
        });

        stack.TickTurn();
        stack.TickTurn();

        Assert.Equal(2, stack.GetSum(AttributeType.Defense));
    }

    // ─── AC4: ClearTemporary 只清除临时层 ───────────────────

    [Fact]
    public void ClearTemporary_OnlyRemovesTemporaryLayer()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "growth:ch1",
            Layer = ModifierLayer.Permanent,
            Attribute = AttributeType.Strength,
            Value = 5
        });

        stack.Add(new AttributeModifier
        {
            Source = "equipment:ring",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Agility,
            Value = 3
        });

        stack.Add(new AttributeModifier
        {
            Source = "buff:haste",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Speed,
            Value = 4,
            Duration = 3
        });

        stack.ClearTemporary();

        Assert.Equal(5, stack.GetSum(AttributeType.Strength));   // Permanent 保留
        Assert.Equal(3, stack.GetSum(AttributeType.Agility));    // SemiPermanent 保留
        Assert.Equal(0, stack.GetSum(AttributeType.Speed));      // Temporary 已清除
        Assert.Equal(2, stack.Count);
    }

    // ─── AC5: 钳位测试（负值修改器） ────────────────────────

    [Fact]
    public void GetSum_CanReturnNegative_ClampingIsCallerResponsibility()
    {
        // ModifierStack 只负责计算 sum，钳位由调用方（CharacterAttributes）负责
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "debuff:weakness",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Strength,
            Value = -20,
            Duration = 2
        });

        Assert.Equal(-20, stack.GetSum(AttributeType.Strength));
    }

    [Fact]
    public void NegativeModifier_WithPositive_NetResult()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "equipment:gauntlets",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Strength,
            Value = 5
        });

        stack.Add(new AttributeModifier
        {
            Source = "debuff:poison",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Strength,
            Value = -8,
            Duration = 3
        });

        // 5 + (-8) = -3 → 调用方负责钳位到1
        Assert.Equal(-3, stack.GetSum(AttributeType.Strength));
    }

    // ─── AC6: Remove(source) 精确移除 ──────────────────────

    [Fact]
    public void Remove_BySource_RemovesAllFromThatSource()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "equipment:full_set",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Strength,
            Value = 3
        });

        stack.Add(new AttributeModifier
        {
            Source = "equipment:full_set",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Defense,
            Value = 5
        });

        stack.Add(new AttributeModifier
        {
            Source = "buff:other",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Speed,
            Value = 2,
            Duration = 1
        });

        stack.Remove("equipment:full_set");

        Assert.Equal(0, stack.GetSum(AttributeType.Strength));
        Assert.Equal(0, stack.GetSum(AttributeType.Defense));
        Assert.Equal(2, stack.GetSum(AttributeType.Speed)); // 未受影响
        Assert.Equal(1, stack.Count);
    }

    // ─── 额外边界测试 ───────────────────────────────────────

    [Fact]
    public void GetSum_NoModifiers_ReturnsZero()
    {
        var stack = new ModifierStack();
        Assert.Equal(0, stack.GetSum(AttributeType.Attack));
    }

    [Fact]
    public void HasSource_ReturnsTrueWhenExists()
    {
        var stack = new ModifierStack();
        stack.Add(new AttributeModifier
        {
            Source = "growth:ch2_node1",
            Layer = ModifierLayer.Permanent,
            Attribute = AttributeType.InnerPower,
            Value = 4
        });

        Assert.True(stack.HasSource("growth:ch2_node1"));
        Assert.False(stack.HasSource("growth:ch3_node1"));
    }

    [Fact]
    public void GetByLayer_ReturnsCorrectSubset()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "growth:ch1",
            Layer = ModifierLayer.Permanent,
            Attribute = AttributeType.Strength,
            Value = 3
        });

        stack.Add(new AttributeModifier
        {
            Source = "buff:x",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Speed,
            Value = 2,
            Duration = 1
        });

        var permanent = stack.GetByLayer(ModifierLayer.Permanent);
        Assert.Single(permanent);
        Assert.Equal("growth:ch1", permanent[0].Source);
    }

    [Fact]
    public void MultipleAttributes_SameSource_TrackedIndependently()
    {
        var stack = new ModifierStack();

        stack.Add(new AttributeModifier
        {
            Source = "equipment:heavy_armor",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Defense,
            Value = 10
        });

        stack.Add(new AttributeModifier
        {
            Source = "equipment:heavy_armor",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Speed,
            Value = -2
        });

        Assert.Equal(10, stack.GetSum(AttributeType.Defense));
        Assert.Equal(-2, stack.GetSum(AttributeType.Speed));
        Assert.Equal(2, stack.Count); // 不同属性，各算各的
    }
}
