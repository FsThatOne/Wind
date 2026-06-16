using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Mindset;

public sealed class MindsetDialogueBridge
{
    private readonly MindsetService _mindsetService;
    private readonly List<MindsetShift> _pendingShifts = new();
    private readonly List<string> _errors = new();
    private readonly Action _unsubscribe;

    public MindsetDialogueBridge(IEventBus eventBus, MindsetService mindsetService)
    {
        _mindsetService = mindsetService;
        _unsubscribe = eventBus.Subscribe<DialogueMindsetShiftEvent>(Enqueue);
    }

    public int PendingCount => _pendingShifts.Count;

    public IReadOnlyList<string> Errors => _errors;

    public void Dispose()
    {
        _unsubscribe();
    }

    public int ApplyPending()
    {
        if (_pendingShifts.Count == 0)
            return 0;

        var count = _pendingShifts.Count;
        _mindsetService.ApplyShifts(_pendingShifts);
        _pendingShifts.Clear();
        return count;
    }

    public void Clear()
    {
        _pendingShifts.Clear();
        _errors.Clear();
    }

    private void Enqueue(DialogueMindsetShiftEvent dialogueEvent)
    {
        if (!TryParseAxis(dialogueEvent.Axis, out var axis))
        {
            _errors.Add($"未知心境轴: '{dialogueEvent.Axis}'");
            return;
        }

        _pendingShifts.Add(new MindsetShift(axis, dialogueEvent.Delta));
    }

    public static bool TryParseAxis(string axisName, out MindsetAxis axis)
    {
        switch (axisName.Trim().ToLowerInvariant())
        {
            case "resolve":
                axis = MindsetAxis.Resolve;
                return true;
            case "worldly":
                axis = MindsetAxis.Worldly;
                return true;
            case "morality":
                axis = MindsetAxis.Morality;
                return true;
            default:
                axis = default;
                return false;
        }
    }
}
