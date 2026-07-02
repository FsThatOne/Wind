using FengZhi.Foundation.Dialogue;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Godot;

namespace FengZhi.Dialogue;

public partial class DialogueManager : Node
{
	private readonly DialogueConfigLoader _loader = new();

	private DialogueRuntime? _runtime;
	private DialogueUiPresenter? _presenter;
	private DialogueEventQueue? _eventQueue;
	private MindsetDialogueBridge? _mindsetBridge;
	private DialogueCodePhraseBook? _codePhraseBook;

	private IEventBus _eventBus = null!;
	private MindsetService _mindsetService = null!;
	private IDialogueConditionValueProvider _conditionProvider = null!;
	private DialoguePanel _panel = null!;

	private int _charsPerSecond = 30;
	private double _tickAccumulator;

	public bool IsDialogueActive =>
		_runtime != null &&
		_runtime.State != DialogueRuntimeState.Idle;

	public bool IsInChoiceMode =>
		_runtime?.State == DialogueRuntimeState.ProcessingChoice;

	[Signal]
	public delegate void DialogueEndedEventHandler();

	public void Initialize(IEventBus eventBus, MindsetService mindsetService, DialoguePanel panel, IDialogueConditionValueProvider conditionProvider)
	{
		_eventBus = eventBus;
		_mindsetService = mindsetService;
		_panel = panel;
		_conditionProvider = conditionProvider;

		var gameFlow = GetNodeOrNull<GameFlow>("/root/GameFlow");
		_charsPerSecond = gameFlow?.TextCharsPerSecond ?? 30;
	}

	public void StartDialogue(string resPath)
	{
		using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr($"[DialogueManager] Cannot open: {resPath}");
			return;
		}

		var yaml = file.GetAsText();
		var sequence = _loader.LoadSequence(yaml, resPath);

		_eventQueue = new DialogueEventQueue(_eventBus);
		var evaluator = new DialogueConditionEvaluator(_conditionProvider);
		_mindsetBridge = new MindsetDialogueBridge(_eventBus, _mindsetService);

		_codePhraseBook ??= new DialogueCodePhraseBook();
		var codePhraseProvider = new DialogueCodePhraseProvider(_codePhraseBook, sequence);

		_runtime = new DialogueRuntime(
			conditionEvaluator: evaluator,
			eventQueue: _eventQueue,
			codePhraseBook: _codePhraseBook,
			codePhraseProvider: codePhraseProvider)
		{
			CharactersPerTick = 1
		};

		_presenter = new DialogueUiPresenter(_runtime);
		_tickAccumulator = 0;
		_runtime.Start(sequence);

		GD.Print($"[DialogueManager] Started: {resPath}");
	}

	public override void _Process(double delta)
	{
		if (_presenter == null || _runtime == null) return;

		if (_charsPerSecond >= 1000)
		{
			for (int i = 0; i < 200; i++)
				_presenter.Tick();
		}
		else
		{
			_tickAccumulator += delta;
			var interval = 1.0 / _charsPerSecond;
			int ticks = 0;
			while (_tickAccumulator >= interval && ticks < 10)
			{
				_presenter.Tick();
				_tickAccumulator -= interval;
				ticks++;
			}

			if (_tickAccumulator > interval * 3)
				_tickAccumulator = 0;
		}

		var snapshot = _presenter.GetSnapshot();
		_panel.ApplySnapshot(snapshot);

		if (_runtime.State == DialogueRuntimeState.Exiting)
		{
			_runtime.CompleteExit();
			FinishDialogue();
		}
	}

	private void FinishDialogue()
	{
		var dispatched = _runtime!.RestoreWorldAndDispatchEvents();
		var applied = _mindsetBridge?.ApplyPending() ?? 0;

		if (dispatched > 0 || applied > 0)
		{
			GD.Print($"[DialogueManager] End. Events dispatched: {dispatched}, mindset shifts applied: {applied}");
			GD.Print($"[DialogueManager] Mindset state: R={_mindsetService.State.Resolve} W={_mindsetService.State.Worldly} M={_mindsetService.State.Morality}");
		}

		_mindsetBridge?.Dispose();
		_mindsetBridge = null;
		_runtime = null;
		_presenter = null;

		_panel.ApplySnapshot(null);
		EmitSignal(SignalName.DialogueEnded);
	}

	public void HandleConfirm()
	{
		if (_presenter == null) return;

		if (IsInChoiceMode)
		{
			_presenter.ConfirmSelection(DialogueUiInputSource.KeyboardMouse);
		}
		else
		{
			_presenter.HandleInput(DialogueUiInputIntent.Confirm, DialogueUiInputSource.KeyboardMouse);
		}
	}

	public void HandleMoveSelection(int delta)
	{
		_presenter?.MoveSelection(delta, DialogueUiInputSource.KeyboardMouse);
	}

	public void HandleInvestigate()
	{
		_presenter?.InvestigateInsight(DialogueUiInputSource.KeyboardMouse);
	}
}
