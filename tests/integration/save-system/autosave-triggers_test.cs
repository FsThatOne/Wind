using FengZhi.Foundation.Events;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Integration.SaveSystem;

public sealed class AutosaveTriggersTests
{
    [Fact]
    public async Task RequestAutosaveAsync_SceneTransitionCompleted_Waits500MsBeforeAutosave()
    {
        var saveManager = new RecordingSaveManager();
        var delay = new ControlledDelay();
        var scheduler = new AutosaveScheduler(saveManager, delay: delay.DelayAsync);

        var request = scheduler.RequestAutosaveAsync(AutosaveTriggerReason.SceneTransitionCompleted);
        await delay.WaitUntilRequestedAsync();

        Assert.Equal(AutosaveScheduler.DefaultTriggerDelay, delay.RequestedDelay);
        Assert.Empty(saveManager.RequestedSlots);

        delay.Complete();
        var result = await request;

        Assert.True(result.Success, result.Error);
        Assert.Equal(new[] { SaveSlotId.AutoSave }, saveManager.RequestedSlots);
    }

    [Theory]
    [InlineData(AutosaveTriggerReason.InnRestCompleted)]
    [InlineData(AutosaveTriggerReason.ChapterAdvanced)]
    [InlineData(AutosaveTriggerReason.LockedDialogueCompleted)]
    public async Task RequestAutosaveAsync_SupportedTriggers_SaveToAutosaveSlot(AutosaveTriggerReason reason)
    {
        var saveManager = new RecordingSaveManager();
        var scheduler = new AutosaveScheduler(saveManager, delay: ImmediateDelay);

        var result = await scheduler.RequestAutosaveAsync(reason);

        Assert.True(result.Success, result.Error);
        Assert.Equal(new[] { SaveSlotId.AutoSave }, saveManager.RequestedSlots);
    }

    [Theory]
    [InlineData("combat")]
    [InlineData("cutscene")]
    [InlineData("locked_dialogue")]
    public async Task RequestAutosaveAsync_WhenBlocked_DoesNotWrite(string reason)
    {
        var saveManager = new RecordingSaveManager();
        var blocker = new AutosaveBlocker(true, reason);
        var scheduler = new AutosaveScheduler(saveManager, blocker, ImmediateDelay);

        var result = await scheduler.RequestAutosaveAsync(AutosaveTriggerReason.SceneTransitionCompleted);

        Assert.False(result.Success);
        Assert.Contains(reason, result.Error);
        Assert.Empty(saveManager.RequestedSlots);
    }

    [Fact]
    public async Task RequestAutosaveAsync_DuringManualSave_QueuesUntilManualSaveCompletes()
    {
        var saveManager = new RecordingSaveManager();
        var scheduler = new AutosaveScheduler(saveManager, delay: ImmediateDelay);

        var manualStarted = saveManager.HoldNextSaveAsync();
        var manual = scheduler.SaveManualAsync(SaveSlotId.Manual(1));
        await manualStarted;

        var autosave = scheduler.RequestAutosaveAsync(AutosaveTriggerReason.ChapterAdvanced);
        await Task.Delay(20);

        Assert.Equal(new[] { SaveSlotId.Manual(1) }, saveManager.RequestedSlots);

        saveManager.ReleaseHeldSave();
        Assert.True((await manual).Success);
        Assert.True((await autosave).Success);
        Assert.Equal(new[] { SaveSlotId.Manual(1), SaveSlotId.AutoSave }, saveManager.RequestedSlots);
    }

    [Fact]
    public async Task RequestAutosaveAsync_MultipleTriggersWhileQueued_CoalescesToOneAutosave()
    {
        var saveManager = new RecordingSaveManager();
        var delay = new ControlledDelay();
        var scheduler = new AutosaveScheduler(saveManager, delay: delay.DelayAsync);

        var first = scheduler.RequestAutosaveAsync(AutosaveTriggerReason.SceneTransitionCompleted);
        await delay.WaitUntilRequestedAsync();
        var second = await scheduler.RequestAutosaveAsync(AutosaveTriggerReason.InnRestCompleted);

        Assert.True(second.Success, second.Error);
        Assert.Empty(saveManager.RequestedSlots);

        delay.Complete();
        Assert.True((await first).Success);
        Assert.Equal(new[] { SaveSlotId.AutoSave }, saveManager.RequestedSlots);
    }

    [Fact]
    public async Task AutosaveTriggerEvent_WhenPublished_RequestsAutosave()
    {
        var eventBus = new EventBus();
        var saveManager = new RecordingSaveManager();
        var scheduler = new AutosaveScheduler(saveManager, delay: ImmediateDelay);
        using var subscription = new Subscription(scheduler.Subscribe(eventBus));

        eventBus.Publish(new AutosaveTriggerEvent(AutosaveTriggerReason.LockedDialogueCompleted));
        await saveManager.WaitForSaveCountAsync(1);

        Assert.Equal(new[] { SaveSlotId.AutoSave }, saveManager.RequestedSlots);
    }

    [Fact]
    public async Task RequestAutosaveAsync_Success_MetadataIdentifiesAutosaveSlot()
    {
        var store = new InMemorySavePayloadStore();
        var manager = new SaveManager(store, new EventBus());
        var scheduler = new AutosaveScheduler(manager, delay: ImmediateDelay);

        var result = await scheduler.RequestAutosaveAsync(AutosaveTriggerReason.SceneTransitionCompleted);

        Assert.True(result.Success, result.Error);
        var metadata = manager.GetMetadata(SaveSlotId.AutoSave);
        Assert.True(metadata.IsOccupied);
        Assert.Equal(SaveSlotKind.Auto, metadata.SlotId.Kind);
        Assert.Equal("autosave", metadata.SlotId.Name);
    }

    private static Task ImmediateDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private sealed class ControlledDelay
    {
        private readonly TaskCompletionSource _requested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeSpan RequestedDelay { get; private set; }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelay = delay;
            _requested.SetResult();
            return _completed.Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilRequestedAsync() => _requested.Task;

        public void Complete() => _completed.SetResult();
    }

    private sealed class RecordingSaveManager : ISaveManager
    {
        private readonly object _sync = new();
        private readonly TaskCompletionSource _saveObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource? _heldSave;
        private TaskCompletionSource? _heldSaveStarted;

        public List<SaveSlotId> RequestedSlots { get; } = new();

        public SaveResult RegisterSerializer(ISaveable saveable) => SaveResult.Ok();

        public async Task<SaveResult> SaveGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource? heldSave = null;
            lock (_sync)
            {
                RequestedSlots.Add(slotId);
                heldSave = _heldSave;
                _heldSaveStarted?.TrySetResult();
                _saveObserved.TrySetResult();
            }

            if (heldSave != null)
                await heldSave.Task.WaitAsync(cancellationToken);

            return SaveResult.Ok(slotId);
        }

        public Task<SaveResult> LoadGameAsync(SaveSlotId slotId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SaveResult.Ok(slotId));
        }

        public IReadOnlyList<SlotMetadata> ListSlots() => Array.Empty<SlotMetadata>();

        public SaveResult DeleteSlot(SaveSlotId slotId) => SaveResult.Ok(slotId);

        public SlotMetadata GetMetadata(SaveSlotId slotId) => SlotMetadata.Empty(slotId);

        public bool IsSlotOccupied(SaveSlotId slotId) => false;

        public Task HoldNextSaveAsync()
        {
            _heldSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _heldSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _heldSaveStarted.Task;
        }

        public void ReleaseHeldSave()
        {
            _heldSave?.SetResult();
            _heldSave = null;
        }

        public Task WaitForSaveCountAsync(int count)
        {
            lock (_sync)
            {
                if (RequestedSlots.Count >= count)
                    return Task.CompletedTask;
            }

            return _saveObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose() => _unsubscribe();
    }
}
