using Godot;
using FengZhi.Foundation.Settings;

namespace FengZhi.Settings;

public partial class SettingsManager : Node
{
    public SettingsService Settings { get; private set; } = null!;
    public PauseMenuService PauseMenu { get; private set; } = null!;

    private static readonly PackedScene PauseMenuScene =
        GD.Load<PackedScene>("res://scenes/ui/PauseMenu.tscn");

    private Control? _pauseMenuInstance;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var persistence = new SettingsConfigFilePersistence();
        var applier = new GodotSettingsApplier(GetTree());
        Settings = new SettingsService(persistence, applier);
        Settings.Load();

        PauseMenu = new PauseMenuService(new CinematicLockQuery(GetTree()));
        PauseMenu.Paused += OnPaused;
        PauseMenu.Resumed += OnResumed;
    }

    public override void _Process(double delta)
    {
        Settings.Tick((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            if (PauseMenu.IsPaused)
            {
                ClosePauseMenu();
            }
            else
            {
                PauseMenu.TryPause();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public void ClosePauseMenu()
    {
        PauseMenu.Resume();
    }

    private void OnPaused()
    {
        GetTree().Paused = true;
        ShowPauseMenuUi();
    }

    private void OnResumed()
    {
        GetTree().Paused = false;
        HidePauseMenuUi();
    }

    private void ShowPauseMenuUi()
    {
        if (_pauseMenuInstance != null) return;
        _pauseMenuInstance = PauseMenuScene.Instantiate<Control>();
        GetTree().Root.AddChild(_pauseMenuInstance);
    }

    private void HidePauseMenuUi()
    {
        if (_pauseMenuInstance == null) return;
        _pauseMenuInstance.QueueFree();
        _pauseMenuInstance = null;
    }

    private sealed class CinematicLockQuery : ICinematicLockQuery
    {
        private readonly SceneTree _tree;
        public CinematicLockQuery(SceneTree tree) => _tree = tree;

        public bool IsInCinematicLock()
        {
            var gameFlow = _tree.Root.GetNodeOrNull<Node>("/root/GameFlow");
            if (gameFlow == null) return false;
            return (bool)gameFlow.Get("IsInCinematicLock");
        }
    }
}
