using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 性格模板：定义一类敌人的体系权重偏好和弱点。
/// GDD §Core Rules 2: 性格模板系统。
/// </summary>
public sealed class PersonalityTemplate
{
    public string Name { get; }

    /// <summary>刚系基础权重</summary>
    public int WeightGang { get; }

    /// <summary>柔系基础权重</summary>
    public int WeightRou { get; }

    /// <summary>巧系基础权重</summary>
    public int WeightQiao { get; }

    /// <summary>弱点体系：被此体系击中时额外 +1 破绽</summary>
    public MoveType WeaknessType { get; }

    /// <summary>是否启用反读</summary>
    public bool CounterReadEnabled { get; }

    /// <summary>反读基础概率 (0-1)</summary>
    public float CounterReadChance { get; }

    public PersonalityTemplate(
        string name,
        int weightGang,
        int weightRou,
        int weightQiao,
        MoveType weaknessType,
        bool counterReadEnabled = false,
        float counterReadChance = 0f)
    {
        if (weightGang < 0 || weightRou < 0 || weightQiao < 0)
            throw new ArgumentException("权重不能为负");

        Name = name;
        WeightGang = weightGang;
        WeightRou = weightRou;
        WeightQiao = weightQiao;
        WeaknessType = weaknessType;
        CounterReadEnabled = counterReadEnabled;
        CounterReadChance = counterReadChance;
    }

    /// <summary>
    /// 获取指定体系的基础权重。
    /// </summary>
    public int GetBaseWeight(MoveType type) => type switch
    {
        MoveType.Gang => WeightGang,
        MoveType.Rou => WeightRou,
        MoveType.Qiao => WeightQiao,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // --- 预设模板 ---

    /// <summary>刚猛型：60/15/25，弱柔。山贼、重甲卫士。</summary>
    public static PersonalityTemplate Fierce { get; } = new(
        "刚猛型", 60, 15, 25, MoveType.Rou);

    /// <summary>柔韧型：15/55/30，弱巧。僧侣、内功高手。</summary>
    public static PersonalityTemplate Resilient { get; } = new(
        "柔韧型", 15, 55, 30, MoveType.Qiao);

    /// <summary>诡诈型：25/20/55，弱刚。刺客、轻功高手。</summary>
    public static PersonalityTemplate Cunning { get; } = new(
        "诡诈型", 25, 20, 55, MoveType.Gang);
}
