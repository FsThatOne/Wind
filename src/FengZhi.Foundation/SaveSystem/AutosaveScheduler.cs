using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 自动存档调度器，负责触发延迟、阻断条件和与手动存档的串行化。
/// </summary>
public sealed partial class AutosaveScheduler
{
    /// <summary>ADR-0004 要求的自动存档触发延迟。</summary>
    public static readonly TimeSpan DefaultTriggerDelay = TimeSpan.FromMilliseconds(500);

    private readonly ISaveManager _saveManager;
    private readonly IAutosaveBlocker _blocker;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly object _sync = new();
    private bool _autosaveQueued;

    /// <summary>创建自动存档调度器。</summary>
    public AutosaveScheduler(
        ISaveManager saveManager,
        IAutosaveBlocker? blocker = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
        _blocker = blocker ?? AutosaveBlocker.None;
        _delay = delay ?? Task.Delay;
    }

    /// <summary>订阅自动存档触发事件。返回取消订阅 action。</summary>
    public Action Subscribe(IEventBus eventBus)
    {
        if (eventBus == null)
            throw new ArgumentNullException(nameof(eventBus));

        return eventBus.Subscribe<AutosaveTriggerEvent>(e => _ = RequestAutosaveAsync(e.Reason));
    }

    /// <summary>手动存档。所有存档写入共用同一个 gate，避免并发写同一文件。</summary>
    public Task<SaveResult> SaveManualAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
    {
        if (slotId.Kind != SaveSlotKind.Manual)
            return Task.FromResult(SaveResult.Fail("SaveManualAsync 只接受手动槽位。", slotId));

        return SaveSlotWithGateAsync(slotId, cancellationToken);
    }

    /// <summary>
    /// 请求一次自动存档。连续触发会合并为最多一个等待中的自动存档。
    /// </summary>
    public async Task<SaveResult> RequestAutosaveAsync(
        AutosaveTriggerReason reason,
        CancellationToken cancellationToken = default)
    {
        if (!AutosaveTriggerRules.IsSupported(reason))
            return SaveResult.Fail($"不支持的自动存档触发原因：{reason}", SaveSlotId.AutoSave);

        lock (_sync)
        {
            if (_autosaveQueued)
                return SaveResult.Ok(SaveSlotId.AutoSave);

            _autosaveQueued = true;
        }

        try
        {
            await _delay(DefaultTriggerDelay, cancellationToken).ConfigureAwait(false);

            if (_blocker.IsAutosaveBlocked)
                return SaveResult.Fail($"自动存档被阻断：{_blocker.BlockReason}", SaveSlotId.AutoSave);

            return await SaveSlotWithGateAsync(SaveSlotId.AutoSave, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_sync)
                _autosaveQueued = false;
        }
    }

    private async Task<SaveResult> SaveSlotWithGateAsync(SaveSlotId slotId, CancellationToken cancellationToken)
    {
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _saveManager.SaveGameAsync(slotId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _saveGate.Release();
        }
    }
}

/// <summary>
/// 自动存档阻断状态。后续可由战斗、演出、锁定对话和存档状态聚合实现。
/// </summary>
public interface IAutosaveBlocker
{
    /// <summary>当前是否禁止自动存档。</summary>
    bool IsAutosaveBlocked { get; }

    /// <summary>阻断原因，用于日志和测试。</summary>
    string BlockReason { get; }
}

/// <summary>无阻断状态。</summary>
public sealed partial class AutosaveBlocker : IAutosaveBlocker
{
    /// <summary>永不阻断的默认实例。</summary>
    public static AutosaveBlocker None { get; } = new(false, string.Empty);

    /// <summary>创建阻断状态。</summary>
    public AutosaveBlocker(bool isAutosaveBlocked, string blockReason)
    {
        IsAutosaveBlocked = isAutosaveBlocked;
        BlockReason = blockReason;
    }

    /// <inheritdoc />
    public bool IsAutosaveBlocked { get; }

    /// <inheritdoc />
    public string BlockReason { get; }
}

/// <summary>自动存档触发原因。</summary>
public enum AutosaveTriggerReason
{
    /// <summary>场景切换完成。</summary>
    SceneTransitionCompleted,

    /// <summary>客栈休息完成。</summary>
    InnRestCompleted,

    /// <summary>主线章节推进。</summary>
    ChapterAdvanced,

    /// <summary>锁定对话序列完成。</summary>
    LockedDialogueCompleted
}

/// <summary>自动存档触发规则。</summary>
public static partial class AutosaveTriggerRules
{
    /// <summary>该原因是否会触发自动存档。</summary>
    public static bool IsSupported(AutosaveTriggerReason reason)
    {
        return reason is AutosaveTriggerReason.SceneTransitionCompleted
            or AutosaveTriggerReason.InnRestCompleted
            or AutosaveTriggerReason.ChapterAdvanced
            or AutosaveTriggerReason.LockedDialogueCompleted;
    }
}

/// <summary>请求自动存档的跨系统事件。</summary>
public sealed partial record AutosaveTriggerEvent(AutosaveTriggerReason Reason) : GameEvent;
