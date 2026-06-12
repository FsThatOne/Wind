using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 招式成长状态管理服务。纯逻辑 POCO，不依赖引擎。
/// 管理角色持有的招式成长进度（习得、残卷收集、合成、升级、批注覆盖）。
/// </summary>
public sealed class MoveProgressionService
{
    private readonly IDataTable<MoveDefinition> _moveTable;
    private readonly Dictionary<string, MoveProgressionEntry> _entries = new(StringComparer.Ordinal);

    public MoveProgressionService(IDataTable<MoveDefinition> moveTable)
    {
        _moveTable = moveTable ?? throw new ArgumentNullException(nameof(moveTable));
    }

    /// <summary>获取指定招式的成长状态，未习得时返回 null。</summary>
    public MoveProgressionEntry? GetEntry(string moveId) =>
        _entries.TryGetValue(moveId, out var entry) ? entry : null;

    /// <summary>获取所有已习得招式的成长状态。</summary>
    public IReadOnlyCollection<MoveProgressionEntry> GetAll() => _entries.Values;

    /// <summary>
    /// 习得普通武学。普通武学习得即满 completion=1.00。
    /// </summary>
    public ProgressionResult LearnBasicMove(string moveId)
    {
        var def = _moveTable.Get(moveId);
        if (def == null) return ProgressionResult.MoveNotFound;
        if (def.Category != MoveCategory.Basic) return ProgressionResult.InvalidStageForOperation;
        if (_entries.ContainsKey(moveId)) return ProgressionResult.AlreadyLearned;

        _entries[moveId] = new MoveProgressionEntry(moveId, MoveProgressionStage.Mastered);
        return ProgressionResult.Success;
    }

    /// <summary>
    /// 获得一张高级/绝学残卷。第 1 张时创建 Fragment 状态，
    /// 超过合成需求（3 张）或已达更高阶段时记录为多余残卷。
    /// </summary>
    public ProgressionResult AddFragment(string moveId)
    {
        var def = _moveTable.Get(moveId);
        if (def == null) return ProgressionResult.MoveNotFound;
        if (def.Category == MoveCategory.Basic) return ProgressionResult.InvalidStageForOperation;

        if (!_entries.TryGetValue(moveId, out var entry))
        {
            // 第 1 张残卷：创建 Fragment 状态
            entry = new MoveProgressionEntry(moveId, MoveProgressionStage.Fragment, fragmentCount: 1);
            _entries[moveId] = entry;
            return ProgressionResult.Success;
        }

        // 已达拓本或更高阶段 → 多余残卷
        if (entry.Stage >= MoveProgressionStage.Manuscript)
        {
            entry.AddExcessFragment();
            return ProgressionResult.ExcessFragment;
        }

        // Fragment 阶段，累加残卷
        var newCount = entry.FragmentCount + 1;
        if (newCount > ProgressionCompletion.FragmentsRequiredForSynthesis)
        {
            entry.AddExcessFragment();
            return ProgressionResult.ExcessFragment;
        }

        entry.SetFragmentCount(newCount);
        return ProgressionResult.Success;
    }

    /// <summary>
    /// 合成拓本：3 张同招式残卷 → 拓本 (completion=0.72)。
    /// </summary>
    public ProgressionResult SynthesizeToManuscript(string moveId)
    {
        var def = _moveTable.Get(moveId);
        if (def == null) return ProgressionResult.MoveNotFound;

        if (!_entries.TryGetValue(moveId, out var entry))
            return ProgressionResult.MoveNotFound;

        if (entry.Stage != MoveProgressionStage.Fragment)
            return ProgressionResult.InvalidStageForOperation;

        if (entry.FragmentCount < ProgressionCompletion.FragmentsRequiredForSynthesis)
            return ProgressionResult.InsufficientFragments;

        entry.SetStage(MoveProgressionStage.Manuscript);
        return ProgressionResult.Success;
    }

    /// <summary>
    /// 升级为完本 (completion=0.88)，解锁高级特效/绝学特效 v1。
    /// </summary>
    public ProgressionResult UpgradeToComplete(string moveId)
    {
        if (!_entries.TryGetValue(moveId, out var entry))
            return ProgressionResult.MoveNotFound;

        if (entry.Stage != MoveProgressionStage.Manuscript)
            return ProgressionResult.InvalidStageForOperation;

        entry.SetStage(MoveProgressionStage.Complete);
        return ProgressionResult.Success;
    }

    /// <summary>
    /// 叙事事件提升为真传 (completion=1.00)，绝学特效 v2 解锁。
    /// </summary>
    public ProgressionResult UpgradeToMastered(string moveId)
    {
        if (!_entries.TryGetValue(moveId, out var entry))
            return ProgressionResult.MoveNotFound;

        if (entry.Stage != MoveProgressionStage.Complete)
            return ProgressionResult.InvalidStageForOperation;

        entry.SetStage(MoveProgressionStage.Mastered);
        return ProgressionResult.Success;
    }

    /// <summary>
    /// 批注版奇遇触发。覆盖当前进度为 Annotated 终态 (completion=1.00)。
    /// 未习得时也可直接获得批注版。
    /// </summary>
    public ProgressionResult ApplyAnnotation(string moveId)
    {
        var def = _moveTable.Get(moveId);
        if (def == null) return ProgressionResult.MoveNotFound;
        if (!def.Annotatable || def.Annotation == null) return ProgressionResult.NotAnnotatable;

        if (_entries.TryGetValue(moveId, out var entry))
        {
            if (entry.Stage == MoveProgressionStage.Annotated)
                return ProgressionResult.AlreadyTerminal;

            // 批注版覆盖当前进度
            entry.SetStage(MoveProgressionStage.Annotated);
        }
        else
        {
            // 未习得直接获得批注版
            _entries[moveId] = new MoveProgressionEntry(moveId, MoveProgressionStage.Annotated);
        }

        return ProgressionResult.Success;
    }
}
