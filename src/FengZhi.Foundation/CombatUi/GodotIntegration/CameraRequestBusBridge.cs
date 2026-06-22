using System;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using Godot;

namespace FengZhi.Foundation.CombatUi.GodotIntegration;

/// <summary>
/// Subscribes to <see cref="CameraRequestBus.ActiveRequestChanged"/> and applies
/// the active request to a target <see cref="Camera2D"/>. When the bus has no
/// active request, restores the default smoothing + zoom captured at construction.
///
/// AC-3 binding: position smoothing toggle + Zoom + GlobalPosition follow-target.
///
/// Looking up the actual follow target node by string id is the caller's
/// responsibility — pass a <see cref="TargetResolver"/> delegate that maps
/// CameraRequest.TargetId to a Node2D world position.
/// </summary>
public sealed class CameraRequestBusBridge : IDisposable
{
    public delegate Vector2? TargetResolver(string targetId);

    private readonly CameraRequestBus _bus;
    private readonly Camera2D _camera;
    private readonly TargetResolver? _resolveTarget;
    private readonly bool _defaultSmoothing;
    private readonly Vector2 _defaultZoom;
    private bool _disposed;

    public CameraRequestBusBridge(CameraRequestBus bus, Camera2D camera, TargetResolver? resolveTarget = null)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _resolveTarget = resolveTarget;
        _defaultSmoothing = camera.PositionSmoothingEnabled;
        _defaultZoom = camera.Zoom;
        _bus.ActiveRequestChanged += OnActiveChanged;
        Apply(_bus.ActiveRequest);
    }

    public bool DefaultSmoothing => _defaultSmoothing;

    public Vector2 DefaultZoom => _defaultZoom;

    private void OnActiveChanged(CameraRequest? req)
    {
        Apply(req);
    }

    private void Apply(CameraRequest? req)
    {
        if (req is null)
        {
            _camera.PositionSmoothingEnabled = _defaultSmoothing;
            _camera.Zoom = _defaultZoom;
            return;
        }

        _camera.PositionSmoothingEnabled = !req.DisableSmoothing;
        _camera.Zoom = new Vector2(req.Zoom, req.Zoom);

        if (_resolveTarget is not null)
        {
            var pos = _resolveTarget(req.TargetId);
            if (pos.HasValue)
            {
                _camera.GlobalPosition = pos.Value;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bus.ActiveRequestChanged -= OnActiveChanged;
        _camera.PositionSmoothingEnabled = _defaultSmoothing;
        _camera.Zoom = _defaultZoom;
    }
}
