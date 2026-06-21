using System;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using Godot;

namespace FengZhi.Foundation.CombatUi.GodotIntegration;

/// <summary>
/// Subscribes to <see cref="TimeScaleController.ScaleChanged"/> and projects
/// the current scale onto <see cref="Engine.TimeScale"/>. Disposing restores
/// <c>Engine.TimeScale = 1.0</c>.
///
/// AC-1 / AC-6 binding: this is the only sanctioned writer of Engine.TimeScale.
/// </summary>
public sealed class TimeScaleEngineBridge : IDisposable
{
    private readonly TimeScaleController _controller;
    private bool _disposed;

    public TimeScaleEngineBridge(TimeScaleController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _controller.ScaleChanged += OnScaleChanged;
        Engine.TimeScale = _controller.CurrentScale;
    }

    private void OnScaleChanged(double scale)
    {
        Engine.TimeScale = scale;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _controller.ScaleChanged -= OnScaleChanged;
        Engine.TimeScale = 1.0d;
    }
}
