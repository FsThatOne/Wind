using Godot;
using System.Collections.Generic;
using System.IO;

namespace FengZhi.Vs;

/// <summary>
/// 自动注入输入事件序列 + 截图捕获，用于无人值守录制视觉证据。
/// 激活条件（满足任一）：
///   - OS.HasFeature("movie")（Movie Maker 模式）
///   - 环境变量 EVIDENCE_CAPTURE=1（截图模式）
/// </summary>
public partial class AutoEvidencePlayer : Node
{
    [Export] public string DemoId { get; set; } = "cu-004";

    private readonly List<(double Time, string Action, bool Pressed)> _script = new();
    private double _elapsed;
    private int _cursor;
    private bool _active;
    private string _outputDir = "";
    private int _screenshotIndex;
    private bool _isScreenshotMode;

    public override void _Ready()
    {
        _isScreenshotMode = OS.GetEnvironment("EVIDENCE_CAPTURE") == "1";
        bool isMovieMode = OS.HasFeature("movie");

        if (!_isScreenshotMode && !isMovieMode)
        {
            QueueFree();
            return;
        }

        _active = true;

        if (_isScreenshotMode)
        {
            _outputDir = OS.GetEnvironment("EVIDENCE_OUTPUT_DIR");
            if (string.IsNullOrEmpty(_outputDir))
            {
                _outputDir = ProjectSettings.GlobalizePath("res://").GetBaseDir()
                    + "/../production/qa/evidence/media";
            }
            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            GD.Print($"[AutoEvidence] 截图模式激活：{DemoId} → {_outputDir}");
        }
        else
        {
            GD.Print($"[AutoEvidence] Movie Maker 模式激活：{DemoId}");
        }

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
                GD.Print($"[AutoEvidence] {DemoId} 完成，退出");
                GetTree().Quit();
                return;
            }

            if (action.StartsWith("__screenshot_"))
            {
                var label = action["__screenshot_".Length..];
                CaptureScreenshot(label);
                continue;
            }

            InjectInput(action, pressed);
        }
    }

    private void CaptureScreenshot(string label)
    {
        _screenshotIndex++;
        var filename = $"{DemoId}_{_screenshotIndex:D2}_{label}.png";
        var fullPath = System.IO.Path.Combine(_outputDir, filename);

        var image = GetViewport().GetTexture().GetImage();
        var err = image.SavePng(fullPath);
        if (err == Error.Ok)
            GD.Print($"[AutoEvidence] 截图 #{_screenshotIndex}: {filename}");
        else
            GD.PushError($"[AutoEvidence] 截图失败: {filename} ({err})");
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

    // cu-004: 招式选择面板 + 预览卡
    private void BuildCu004Script()
    {
        double t = 2.0;
        Screenshot(t, "panel_full_view");

        // 导航到内息不足招式（index 4）
        for (int i = 0; i < 4; i++)
        {
            Press(ref t, "ui_down", 0.1);
            t += 0.6;
        }
        t += 0.3;
        Screenshot(t, "neixi_insufficient_greyed");

        // 继续到心法封印招式（index 5）
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Screenshot(t, "xinfa_sealed_greyed");

        // 继续到使用道具（index 7 = 调息后）
        Press(ref t, "ui_down", 0.1);
        t += 0.3;
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Screenshot(t, "item_unavailable_greyed");

        // 回到顶部克制招式
        for (int i = 0; i < 7; i++)
        {
            Press(ref t, "ui_up", 0.1);
            t += 0.3;
        }
        t += 0.5;
        Screenshot(t, "preview_card_counter");

        // 下移到被克招式
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Screenshot(t, "preview_card_countered");

        // 下移到同系招式
        Press(ref t, "ui_down", 0.1);
        t += 0.8;
        Screenshot(t, "preview_card_neutral");

        // 回到默认焦点
        for (int i = 0; i < 2; i++)
        {
            Press(ref t, "ui_up", 0.1);
            t += 0.3;
        }
        t += 0.5;
        Screenshot(t, "default_focus_first_available");

        t += 1.0;
        Quit(t);
    }

    // cu-005: 反制 + 决胜行动提示
    private void BuildCu005Script()
    {
        double t = 2.0;
        Screenshot(t, "counter_enabled_gold");

        // 按 [ 减内息 3 次 → 内息 < 3
        for (int i = 0; i < 3; i++)
        {
            PressKey(ref t, Key.Bracketleft, 0.1);
            t += 0.5;
        }
        t += 0.3;
        Screenshot(t, "counter_disabled_neixi_low");

        // 按 ] 加内息 3 次 → 恢复
        for (int i = 0; i < 3; i++)
        {
            PressKey(ref t, Key.Bracketright, 0.1);
            t += 0.5;
        }
        t += 0.3;
        Screenshot(t, "counter_restored_gold");

        // 导航到顶部决胜行
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.8;
        Screenshot(t, "decisive_row_highlighted");

        t += 1.0;
        Quit(t);
    }

    // cu-006: 决胜一击演出
    private void BuildCu006Script()
    {
        double t = 2.0;
        Screenshot(t, "before_decisive_trigger");

        // 确认触发演出
        Press(ref t, "ui_accept", 0.1);
        t += 0.5;
        Screenshot(t, "phase1_slowmo_start");

        t += 1.5;
        Screenshot(t, "phase2_camera_push");

        t += 2.0;
        Screenshot(t, "phase3_animation_strike");

        t += 2.0;
        Screenshot(t, "phase4_damage_float_gold");

        // 尝试输入（应该被屏蔽）
        Press(ref t, "ui_down", 0.1);
        t += 0.3;
        Screenshot(t, "input_blocked_during_cinematic");

        t += 3.0;
        Screenshot(t, "phase7_restored_normal");

        t += 2.0;
        Quit(t);
    }

    // cu-008: 双焦点 + D-pad 导航
    private void BuildCu008Script()
    {
        double t = 2.0;
        Screenshot(t, "default_focus_first_row");

        // D-pad 向下循环 8 次
        for (int i = 0; i < 8; i++)
        {
            Press(ref t, "ui_down", 0.1);
            t += 0.4;
        }
        t += 0.3;
        Screenshot(t, "dpad_cycle_wrapped_to_top");

        // D-pad 向上 4 次
        for (int i = 0; i < 4; i++)
        {
            Press(ref t, "ui_up", 0.1);
            t += 0.4;
        }
        t += 0.3;
        Screenshot(t, "dpad_up_navigation");

        // 继续向上到决胜行/反制行
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.3;
        Press(ref t, "ui_up", 0.1);
        t += 0.8;
        Screenshot(t, "decisive_row_reachable_by_dpad");

        // 确认提交
        Press(ref t, "ui_accept", 0.1);
        t += 1.0;
        Screenshot(t, "accept_submits_focused_action");

        t += 1.0;
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

    private void Screenshot(double t, string label)
    {
        _script.Add((t, $"__screenshot_{label}", true));
    }

    private void Quit(double t)
    {
        _script.Add((t, "__quit", true));
    }
}
