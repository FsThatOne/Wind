using Godot;
using FengZhi.Foundation.SaveSystem;
using FengZhi.Save;

namespace FengZhi.Ui;

/// <summary>
/// 存读档菜单 UI。Snapshot-driven 渲染，所有逻辑委托 SaveLoadMenuPresenter。
/// </summary>
public partial class SaveLoadMenuUi : Control
{
    [Export] public int MenuMode { get; set; }

    private SaveLoadMenuPresenter _presenter = null!;
    private Label _titleLabel = null!;
    private VBoxContainer _slotContainer = null!;
    private Button _backButton = null!;
    private Button _deleteButton = null!;
    private Label _messageLabel = null!;
    private ConfirmationDialog _confirmDialog = null!;

    private SaveLoadMenuSnapshot? _lastSnapshot;
    private bool _waitingConfirm;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;

        _titleLabel = GetNode<Label>("MarginContainer/VBoxContainer/TitleLabel");
        _slotContainer = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/SlotContainer");
        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/BottomBar/BackButton");
        _deleteButton = GetNode<Button>("MarginContainer/VBoxContainer/BottomBar/DeleteButton");
        _messageLabel = GetNode<Label>("MarginContainer/VBoxContainer/MessageLabel");
        _confirmDialog = GetNode<ConfirmationDialog>("ConfirmDialog");

        var saveSystem = GetNodeOrNull<SaveSystemNode>("/root/SaveSystem");
        if (saveSystem == null)
        {
            GD.PrintErr("[SaveLoadMenu] SaveSystem autoload not found.");
            QueueFree();
            return;
        }

        var mode = MenuMode == 0 ? SaveLoadMenuMode.Save : SaveLoadMenuMode.Load;
        _presenter = saveSystem.CreatePresenter(mode);
        _titleLabel.Text = mode == SaveLoadMenuMode.Save ? "存档" : "读档";

        _backButton.Pressed += OnBackPressed;
        _deleteButton.Pressed += OnDeletePressed;
        _confirmDialog.Confirmed += OnConfirmDialogConfirmed;
        _confirmDialog.Canceled += OnConfirmDialogCanceled;

        BuildSlotItems();
        RenderSnapshot();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_waitingConfirm) return;

        if (@event.IsActionPressed("ui_up"))
        {
	            HandleInputAsync(SaveLoadMenuInputIntent.MoveUp);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_down"))
        {
	            HandleInputAsync(SaveLoadMenuInputIntent.MoveDown);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
	            HandleInputAsync(SaveLoadMenuInputIntent.Confirm);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private async void HandleInputAsync(SaveLoadMenuInputIntent intent)
    {
        var result = await _presenter.HandleInputAsync(intent, SaveLoadMenuInputSource.KeyboardMouse);
        RenderSnapshot();

        if (result.Success && intent == SaveLoadMenuInputIntent.Confirm)
        {
            var snapshot = _presenter.GetSnapshot();
            if (snapshot.Mode == SaveLoadMenuMode.Load && snapshot.LastMessage == "读档完成。")
            {
                CloseMenu();
            }
        }
    }

    private void BuildSlotItems()
    {
        foreach (var child in _slotContainer.GetChildren())
            child.QueueFree();

        var allSlots = SaveSlotId.AllSlots();
        for (int i = 0; i < allSlots.Count; i++)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(0, 64),
                ClipText = true,
            };
            int index = i;
            button.Pressed += () => OnSlotClicked(index);
            button.FocusEntered += () => OnSlotFocused(index);
            _slotContainer.AddChild(button);
        }
    }

    private void OnSlotClicked(int index)
    {
        _presenter.SelectIndex(index);
	        HandleInputAsync(SaveLoadMenuInputIntent.Confirm);
    }

    private void OnSlotFocused(int index)
    {
        _presenter.SelectIndex(index);
        RenderSnapshot();
    }

    private void RenderSnapshot()
    {
        _lastSnapshot = _presenter.GetSnapshot();
        var snapshot = _lastSnapshot;

        var buttons = _slotContainer.GetChildren();
        for (int i = 0; i < snapshot.Slots.Count && i < buttons.Count; i++)
        {
            var slot = snapshot.Slots[i];
            var button = (Button)buttons[i];

            if (slot.IsOccupied)
            {
                button.Text = $"{slot.Title}  |  {slot.ChapterName}  |  {slot.TimestampText}  |  {slot.PlaytimeText}";
            }
            else
            {
                button.Text = $"{slot.Title}  |  {slot.StatusText}";
            }

            if (slot.IsSelected && !button.HasFocus())
                button.GrabFocus();
        }

        _messageLabel.Text = snapshot.LastMessage ?? "";
        _deleteButton.Disabled = snapshot.SelectedIndex < 0
            || !snapshot.Slots[snapshot.SelectedIndex].IsOccupied;

        if (snapshot.ConfirmationKind != SaveLoadConfirmationKind.None && !_waitingConfirm)
        {
            _waitingConfirm = true;
            _confirmDialog.DialogText = snapshot.ConfirmationText ?? "确认操作？";
            _confirmDialog.PopupCentered();
        }
    }

    private void OnConfirmDialogConfirmed()
    {
        _waitingConfirm = false;
	        HandleInputAsync(SaveLoadMenuInputIntent.Confirm);
    }

    private void OnConfirmDialogCanceled()
    {
        _waitingConfirm = false;
	        HandleInputAsync(SaveLoadMenuInputIntent.Cancel);
    }

    private void OnDeletePressed()
    {
	        HandleInputAsync(SaveLoadMenuInputIntent.Delete);
    }

    private void OnBackPressed()
    {
        CloseMenu();
    }

    private void CloseMenu()
    {
        QueueFree();
    }
}
