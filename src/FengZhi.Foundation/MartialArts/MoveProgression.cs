namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 招式成长阶段。
/// Basic 类招式习得即 Mastered；Advanced/Ultimate 类走 Fragment → Manuscript → Complete → Mastered。
/// Annotated 是任意类别的终态。
/// </summary>
public enum MoveProgressionStage
{
    /// <summary>残卷。高级/绝学初始阶段，completion=0.55。</summary>
    Fragment,

    /// <summary>拓本。3 张残卷合成，completion=0.72。</summary>
    Manuscript,

    /// <summary>完本。completion=0.88，高级特效/绝学特效 v1 解锁。</summary>
    Complete,

    /// <summary>真传。completion=1.00，绝学特效 v2 解锁。</summary>
    Mastered,

    /// <summary>批注版。奇遇获得，completion=1.00，使用批注组件替换。</summary>
    Annotated
}

/// <summary>
/// 每个阶段对应的 completion 值常量。
/// </summary>
public static class ProgressionCompletion
{
    public const float Fragment = 0.55f;
    public const float Manuscript = 0.72f;
    public const float Complete = 0.88f;
    public const float Mastered = 1.00f;
    public const float Annotated = 1.00f;

    /// <summary>合成拓本所需残卷数量。</summary>
    public const int FragmentsRequiredForSynthesis = 3;

    public static float ForStage(MoveProgressionStage stage) => stage switch
    {
        MoveProgressionStage.Fragment => Fragment,
        MoveProgressionStage.Manuscript => Manuscript,
        MoveProgressionStage.Complete => Complete,
        MoveProgressionStage.Mastered => Mastered,
        MoveProgressionStage.Annotated => Annotated,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}

/// <summary>
/// 招式特效解锁等级。
/// </summary>
[Flags]
public enum EffectUnlockFlags
{
    None = 0,

    /// <summary>完本阶段解锁（高级特效 / 绝学特效 v1）。</summary>
    EffectV1 = 1 << 0,

    /// <summary>真传阶段解锁（绝学特效 v2）。</summary>
    EffectV2 = 1 << 1
}

/// <summary>
/// 单条招式的运行时成长状态（存档/角色持有）。POCO，不继承 Node。
/// </summary>
public sealed class MoveProgressionEntry
{
    public string MoveId { get; }
    public MoveProgressionStage Stage { get; private set; }
    public float Completion => ProgressionCompletion.ForStage(Stage);
    public int FragmentCount { get; private set; }
    public int ExcessFragments { get; private set; }
    public EffectUnlockFlags UnlockedEffects { get; private set; }

    public MoveProgressionEntry(string moveId, MoveProgressionStage stage, int fragmentCount = 0)
    {
        MoveId = moveId;
        Stage = stage;
        FragmentCount = fragmentCount;
        UnlockedEffects = ComputeUnlocks(stage);
    }

    internal void SetStage(MoveProgressionStage newStage)
    {
        Stage = newStage;
        UnlockedEffects = ComputeUnlocks(newStage);
    }

    internal void SetFragmentCount(int count) => FragmentCount = count;
    internal void AddExcessFragment() => ExcessFragments++;

    private static EffectUnlockFlags ComputeUnlocks(MoveProgressionStage stage) => stage switch
    {
        MoveProgressionStage.Complete => EffectUnlockFlags.EffectV1,
        MoveProgressionStage.Mastered => EffectUnlockFlags.EffectV1 | EffectUnlockFlags.EffectV2,
        MoveProgressionStage.Annotated => EffectUnlockFlags.EffectV1 | EffectUnlockFlags.EffectV2,
        _ => EffectUnlockFlags.None
    };
}

/// <summary>
/// 招式成长状态变更操作结果。
/// </summary>
public enum ProgressionResult
{
    /// <summary>操作成功。</summary>
    Success,

    /// <summary>招式定义不存在。</summary>
    MoveNotFound,

    /// <summary>招式已习得（重复习得普通武学）。</summary>
    AlreadyLearned,

    /// <summary>已是终态（真传或批注版），不可再升级。</summary>
    AlreadyTerminal,

    /// <summary>残卷数量不足，无法合成。</summary>
    InsufficientFragments,

    /// <summary>当前阶段不允许该操作。</summary>
    InvalidStageForOperation,

    /// <summary>该招式没有批注版本，不可批注。</summary>
    NotAnnotatable,

    /// <summary>获得多余残卷（已超过合成需求或已达更高阶段）。</summary>
    ExcessFragment
}
