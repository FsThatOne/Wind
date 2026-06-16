using FengZhi.Foundation.CharacterData;

namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// 招式预兆序列定义。GDD §Core Rules 7。
/// 每种性格模板有 1-2 个标志性行为序列。
/// </summary>
public sealed class SignaturePattern
{
    /// <summary>序列步骤（如 [Gang, Gang] 表示"刚→刚→任意"中前两步）</summary>
    public IReadOnlyList<MoveType> Steps { get; }

    /// <summary>匹配时的权重加成（默认 25）</summary>
    public int Bonus { get; }

    /// <summary>前 N 回合的额外倍率（默认 4 回合内 ×1.5）</summary>
    public float OpeningMultiplier { get; }

    /// <summary>开场强调持续回合数</summary>
    public int OpeningRounds { get; }

    public SignaturePattern(IReadOnlyList<MoveType> steps, int bonus = 25, float openingMultiplier = 1.5f, int openingRounds = 4)
    {
        if (steps.Count == 0) throw new ArgumentException("序列不能为空");
        Steps = steps;
        Bonus = bonus;
        OpeningMultiplier = openingMultiplier;
        OpeningRounds = openingRounds;
    }

    // --- 预设 ---

    /// <summary>刚猛型：刚→刚</summary>
    public static SignaturePattern Fierce { get; } = new(new[] { MoveType.Gang, MoveType.Gang });

    /// <summary>柔韧型：柔→巧→柔</summary>
    public static SignaturePattern Resilient { get; } = new(new[] { MoveType.Rou, MoveType.Qiao, MoveType.Rou });

    /// <summary>诡诈型：巧→刚→巧</summary>
    public static SignaturePattern Cunning { get; } = new(new[] { MoveType.Qiao, MoveType.Gang, MoveType.Qiao });
}

/// <summary>
/// 招式预兆序列追踪器。
/// 维护序列进度指针，计算当前步骤的权重加成。
/// </summary>
public sealed class SignatureTracker
{
    private readonly SignaturePattern _pattern;
    private int _pointer;

    public SignatureTracker(SignaturePattern pattern)
    {
        _pattern = pattern;
        _pointer = 0;
    }

    /// <summary>当前序列指针位置（0-based）</summary>
    public int Pointer => _pointer;

    /// <summary>序列是否刚完成一轮</summary>
    public bool JustCompleted { get; private set; }

    /// <summary>
    /// 获取当前步骤匹配时对应体系的权重加成。
    /// </summary>
    /// <param name="currentRound">当前回合数（1-based）</param>
    /// <returns>应用于当前序列步骤体系的额外权重</returns>
    public int GetCurrentBonus(int currentRound)
    {
        float multiplier = currentRound <= _pattern.OpeningRounds
            ? _pattern.OpeningMultiplier
            : 1.0f;
        return (int)MathF.Floor(_pattern.Bonus * multiplier);
    }

    /// <summary>
    /// 获取当前步骤期望的体系类型。
    /// </summary>
    public MoveType GetCurrentStepType() => _pattern.Steps[_pointer];

    /// <summary>
    /// 计算当前步骤对各体系的权重加成。
    /// 仅对序列当前步骤的体系施加加成。
    /// </summary>
    public TypeWeights GetSignatureBonus(int currentRound)
    {
        var targetType = _pattern.Steps[_pointer];
        int bonus = GetCurrentBonus(currentRound);
        return new TypeWeights().With(targetType, bonus);
    }

    /// <summary>
    /// 记录 AI 本回合实际选择的体系。
    /// 如果匹配序列下一步，指针前进；否则指针不动。
    /// </summary>
    public void RecordChoice(MoveType actualType)
    {
        JustCompleted = false;

        if (actualType == _pattern.Steps[_pointer])
        {
            _pointer++;
            if (_pointer >= _pattern.Steps.Count)
            {
                _pointer = 0;
                JustCompleted = true;
            }
        }
        // 不匹配时指针不前进也不后退（GDD: "停滞在当前位置"）
    }

    /// <summary>
    /// 重置指针（用于阶段转换等场景）。
    /// </summary>
    public void Reset() => _pointer = 0;
}
