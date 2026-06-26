using Godot;
using System.Collections.Generic;

namespace FengZhi.Vs;

/// <summary>
/// Movie Maker 模式下自动注入输入事件序列，用于无人值守录制视觉证据。
/// 仅在 OS.HasFeature("movie") 时激活。
/// </summary>
public partial class AutoEvidencePlayer : Node
{
    [Export] public string DemoId { get; set; } = "cu-004";

    private readonly List<(double Time, string Action, bool Pressed)> _script = new();
    private double _elapsed;
    private int _cursor;
    private bool _active;

    public override void _Ready()
    {
        if (!OS.HasFeature("movie"))
        {
            QueueFree();
            return;
        }

        _active = true;
        GD.Print($"[AutoEvidence] 激活自动输入：{DemoId}");
        BuildScript();
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        _elapsed += delta;

        while (_cursor < _script.Count && _elapsed >= _script[_cursor].Time)
        {
            var (_, action, pressed) = _script[_cursor];
            _cursor++;

            if (action == "__quit")
            {
                GD.Print("[AutoEvidence] 录制完成，退出");
                GetTree().Quit();
                return;
            }

            InjectInput(action, pressed);
        }
    }

    private void InjectInput(string action, bool pressed)
    {
        if (action.StartsWith("__key_"))
        {
            var keyStr = action["__key_".Length..];
            if (System.Enum.TryParse<Key>(keyStr, out var key))
            {
                var keyEv = new InputEventKey
                {
                    Keycode = key,
                    Pressed = pressed,
                };
                Input.ParseInputEvent(keyEv);
                GD.Print($"[AutoEvidence] t={_elapsed:F2}s → Key.{key} {(pressed ? "↓" : "↑")}");
            }
            return;
        }

        var ev = new InputEventAction
        {
            Action = action,
            Pressed = pressed,
        };
        Input.ParseInputEvent(ev);
        GD.Print($"[AutoEvidence] t={_elapsed:F2}s → {action} {(pressed ? "↓" : "↑")}");
    }

    private void BuildScript()
    {
        switch (DemoId)
        {
            case "cu-004":
                BuildCu004Script();
                break;
            case "cu-005":
                BuildCu005Script();
                break;
            case "cu-006":
                BuildCu006Script();
                break;
            case "cu-008":
                BuildCu008Script();
                break;
            default:
                BuildCu004Script();
                break;
        }
    }

    private void BuildCu004Script()
    {
        double t = 2.0;
        for (int i = 0; i < 7; i++)
        {
            Press(ref t, "ui_down", 0.1);
            t += 1.0;
        }
        Press(ref t, "ui_up", 0.1);
        t += 0.5;
        Press(ref t, "ui_up", 0.1);
        t += 0.5;
        Press(ref t, "ui_accept", 0.1);
        t += 2.0;
        Quit(t);
    }

    private void BuildCu005Script()
    {
        double t = 2.0;
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Press(ref t, "ui_down", 0.1);
        t += 1.5;

        for (int i = 0; i < 3; i++)
        {
            PressKey(ref t, Key.Bracketleft, 0.1);
            t += 0.8;
        }
        t += 1.0;

        for (int i = 0; i < 3; i++)
        {
            PressKey(ref t, Key.Bracketright, 0.1);
            t += 0.8;
        }
        t += 2.0;
        Quit(t);
    }

    private void BuildCu006Script()
    {
        double t = 2.0;
        Press(ref t, "ui_accept", 0.1);
        t += 12.0;
        Quit(t);
    }

    private void BuildCu008Script()
    {
        double t = 2.0;
        for (int i = 0; i < 8; i++)
        {
            Press(ref t, "ui_down", 0.1);
            t += 0.4;
        }
        t += 0.5;
        for (int i = 0; i < 4; i++)
        {
            Press(ref t, "ui_up", 0.1);
            t += 0.4;
        }
        t += 1.0;
        Press(ref t, "ui_accept", 0.1);
        t += 2.0;
        Quit(t);
    }

    private void Press(ref double t, string action, double duration)
    {
        _script.Add((t, action, true));
        _script.Add((t + duration, action, false));
    }

    private void PressKey(ref double t, Key keycode, double duration)
    {
        _script.Add((t, $"__key_{keycode}", true));
        _script.Add((t + duration, $"__key_{keycode}", false));
    }

    private void Quit(double t)
    {
        _script.Add((t, "__quit", true));
    }
}
