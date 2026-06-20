using System.Collections.Generic;
using Godot;

namespace FengZhi;

public partial class StartCaveGame : Node2D
{
    private const int TrainingInsightThreshold = 8;
    private readonly int _playerInsight = 10;

    private readonly Dictionary<string, InteractionDefinition> _interactions = new()
    {
        ["DeskNote"] = new(
            "中央桌",
            "小师弟，若你又把酒坛认成药坛，回来罚你抄剑谱。\n\n她明知道我早分得清了，还是每回都要这样写。"),
        ["TrainingMarks"] = new(
            "练功痕迹",
            "墙上还留着从前练剑时画下的起手式。线条有些歪，却看得出当时改了很多遍。",
            "那一笔是她补的。她总说我腕太硬，剑未出，意先绷住了。"),
        ["BirthdayWine"] = new(
            "寿酒",
            "取得：寿酒"),
        ["SupplyChest"] = new(
            "木箱",
            "取得：止血散 x1"),
        ["HerbBasket"] = new(
            "药篓",
            "这些药材多半是师姐晒的。她总说我分不清辛温寒凉。")
    };

    private Label _promptLabel = null!;
    private Label _inventoryLabel = null!;
    private Panel _messagePanel = null!;
    private Label _messageLabel = null!;
    private Area2D? _focusedArea;
    private bool _hasBirthdayWine;
    private bool _chestLooted;
    private bool _trainingInsightSeen;

    public override void _Ready()
    {
        _promptLabel = GetNode<Label>("UiLayer/PromptLabel");
        _inventoryLabel = GetNode<Label>("UiLayer/InventoryLabel");
        _messagePanel = GetNode<Panel>("UiLayer/MessagePanel");
        _messageLabel = GetNode<Label>("UiLayer/MessagePanel/MessageLabel");

        foreach (var child in GetNode<Node2D>("Interactions").GetChildren())
        {
            if (child is Area2D area)
            {
                area.BodyEntered += body => OnInteractionEntered(area, body);
                area.BodyExited += body => OnInteractionExited(area, body);
            }
        }

        UpdateInventoryLabel();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact"))
        {
            if (_messagePanel.Visible)
            {
                HideMessage();
                return;
            }

            InteractWithFocusedArea();
        }

        if (@event.IsActionPressed("insight") && !_messagePanel.Visible)
        {
            TryInspectFocusedArea();
        }
    }

    private void OnInteractionEntered(Area2D area, Node2D body)
    {
        if (body.Name != "Player")
        {
            return;
        }

        _focusedArea = area;
        UpdatePrompt();
    }

    private void OnInteractionExited(Area2D area, Node2D body)
    {
        if (body.Name != "Player" || _focusedArea != area)
        {
            return;
        }

        _focusedArea = null;
        UpdatePrompt();
    }

    private void InteractWithFocusedArea()
    {
        if (_focusedArea is null)
        {
            return;
        }

        switch (_focusedArea.Name)
        {
            case "BirthdayWine":
                if (_hasBirthdayWine)
                {
                    ShowMessage("寿酒已经取好了，回去莫要耽搁。");
                    return;
                }

                _hasBirthdayWine = true;
                UpdateInventoryLabel();
                ShowMessage(_interactions["BirthdayWine"].Text);
                return;
            case "SupplyChest":
                if (_chestLooted)
                {
                    ShowMessage("已经翻过了，里头只剩些干草和旧绳。");
                    return;
                }

                _chestLooted = true;
                ShowMessage(_interactions["SupplyChest"].Text);
                return;
            default:
                if (_interactions.TryGetValue(_focusedArea.Name, out var interaction))
                {
                    ShowMessage(interaction.Text);
                }

                return;
        }
    }

    private void TryInspectFocusedArea()
    {
        if (_focusedArea?.Name != "TrainingMarks")
        {
            return;
        }

        if (_playerInsight < TrainingInsightThreshold)
        {
            return;
        }

        if (_trainingInsightSeen)
        {
            ShowMessage("师姐补过的那几笔，我已经记下了。");
            return;
        }

        _trainingInsightSeen = true;
        ShowMessage(_interactions["TrainingMarks"].InsightText);
        UpdatePrompt();
    }

    private void ShowMessage(string text)
    {
        _messageLabel.Text = text;
        _messagePanel.Visible = true;
        _promptLabel.Visible = false;
    }

    private void HideMessage()
    {
        _messagePanel.Visible = false;
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (_focusedArea is null || _messagePanel.Visible)
        {
            _promptLabel.Visible = false;
            return;
        }

        var prompt = "E / 空格 调查";
        if (_focusedArea.Name == "TrainingMarks" &&
            _playerInsight >= TrainingInsightThreshold &&
            !_trainingInsightSeen)
        {
            prompt = "E / 空格 调查    F 洞察";
        }

        _promptLabel.Text = prompt;
        _promptLabel.Visible = true;
    }

    private void UpdateInventoryLabel()
    {
        _inventoryLabel.Text = _hasBirthdayWine
            ? "任务物品：寿酒"
            : "任务物品：未取得寿酒";
    }

    private readonly record struct InteractionDefinition(
        string Title,
        string Text,
        string InsightText = "");
}
