using System;
using Godot;
using FengZhi.Foundation.Settings;

namespace FengZhi.Ui;

public partial class PauseMenuUi : Control
{
    private Button _resumeButton = null!;
    private Button _saveButton = null!;
    private Button _loadButton = null!;
    private Button _settingsButton = null!;
    private Button _returnToTitleButton = null!;
    private Button _quitButton = null!;
    private ConfirmationDialog _confirmDialog = null!;

    private Settings.SettingsManager? _settingsManager;
    private Action? _pendingConfirmAction;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;

        _resumeButton = GetNode<Button>("MenuContainer/ResumeButton");
        _saveButton = GetNode<Button>("MenuContainer/SaveButton");
        _loadButton = GetNode<Button>("MenuContainer/LoadButton");
        _settingsButton = GetNode<Button>("MenuContainer/SettingsButton");
        _returnToTitleButton = GetNode<Button>("MenuContainer/ReturnToTitleButton");
        _quitButton = GetNode<Button>("MenuContainer/QuitButton");
        _confirmDialog = GetNode<ConfirmationDialog>("ConfirmDialog");

        _settingsManager = GetNodeOrNull<Settings.SettingsManager>("/root/SettingsManager");

        _resumeButton.Pressed += OnResumePressed;
        _saveButton.Pressed += OnSavePressed;
        _loadButton.Pressed += OnLoadPressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _returnToTitleButton.Pressed += OnReturnToTitlePressed;
        _quitButton.Pressed += OnQuitPressed;
        _confirmDialog.Confirmed += OnConfirmDialogConfirmed;

        ConfigureMenuItems();
        _resumeButton.GrabFocus();
    }

    private void ConfigureMenuItems()
    {
        var gameFlow = GetNodeOrNull<Node>("/root/GameFlow");
        bool inCombat = gameFlow != null && (bool)gameFlow.Get("IsInCombat");

        _saveButton.Visible = !inCombat;
    }

    private void OnResumePressed()
    {
        _settingsManager?.ClosePauseMenu();
    }

    private void OnSavePressed()
    {
        var gameFlow = GetNodeOrNull<Node>("/root/GameFlow");
        gameFlow?.Call("QuickSave");
        _settingsManager?.ClosePauseMenu();
    }

    private void OnLoadPressed()
    {
        var gameFlow = GetNodeOrNull<Node>("/root/GameFlow");
        gameFlow?.Call("QuickLoad");
    }

    private void OnSettingsPressed()
    {
        var settingsPanel = GD.Load<PackedScene>("res://scenes/ui/SettingsPanel.tscn");
        var instance = settingsPanel.Instantiate<Control>();
        GetParent().AddChild(instance);
        Visible = false;
        instance.TreeExiting += () => Visible = true;
    }

    private void OnReturnToTitlePressed()
    {
        _pendingConfirmAction = () =>
        {
            _settingsManager?.ClosePauseMenu();
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://scenes/ui/MainMenu.tscn");
        };
        _confirmDialog.DialogText = "有未保存的进度，确定离开吗？";
        _confirmDialog.PopupCentered();
    }

    private void OnQuitPressed()
    {
        _pendingConfirmAction = () => GetTree().Quit();
        _confirmDialog.DialogText = "有未保存的进度，确定退出吗？";
        _confirmDialog.PopupCentered();
    }

    private void OnConfirmDialogConfirmed()
    {
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }
}
