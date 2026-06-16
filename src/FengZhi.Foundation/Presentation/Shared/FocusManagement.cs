using Godot;

namespace FengZhi.Foundation.Presentation.Shared;

/// <summary>
/// 当前 UI 输入来源。用于区分键鼠、手柄和鼠标反馈。
/// </summary>
public enum InputMode
{
    Keyboard,
    Mouse,
    Gamepad
}

/// <summary>
/// Presentation 层焦点栈契约。具体 Godot Autoload 适配在场景接入时提供。
/// </summary>
public interface IFocusManager
{
    InputMode CurrentMode { get; }

    void PushFocus(Control target);

    void PopFocus();

    void ClearStack();
}
