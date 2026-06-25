using System;
using System.Collections.Generic;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;
using FengZhi.Foundation.CombatUi;
using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;
using FengZhi.Foundation.CombatUi.GodotIntegration;
using Godot;

namespace FengZhi.Vs.Combat;

/// <summary>
/// cu-006-vs-integration · 一击决胜 Godot 适配层。
///
/// 职责：
/// 1. 实例化 Foundation 的 CombatAnimationDirector + 3 个 Bridge：
///    - TimeScaleEngineBridge   → Engine.TimeScale (演出期 1.0 → 0.2 → 1.0)
///    - CameraRequestBusBridge  → battle scene Camera2D (推镜 + 锁目标 + 禁用 smoothing)
///    - CombatCinematicLockInputFilter → _UnhandledInput 短路 (屏蔽战斗输入, whitelist ui_pause/ui_system_back)
/// 2. 订阅 BattleEventBus 的 3 个决胜事件:
///    - DecisiveStrikeStartedEvent     → 显示 overlay, cache PrecomputedDamage
///    - DecisiveStrikePhaseAdvancedEvent → 更新 Phase 文本; Phase5 时显示 damage label (max-size deep-gold)
///    - DecisiveStrikeCompletedEvent   → 隐藏 overlay, 触发 SequenceCompleted 回调
/// 3. 每帧 _Process 调 director.Tick(delta) 推进 phase 计时。
///
/// 触发链路 (从 cu-005 binder 接入)：
///   binder.Submit(BattleAction{ IsDecisiveStrike=true, TargetId=...})
///   → JiangnanBattleGame.SubmitPlayerAction 检测 IsDecisiveStrike=true
///   → adapter.RequestDecisive(sourceId, targetId, damage, moveType)
///   → director.RequestDecisiveStrike → 7-phase sequence
///   → adapter.SequenceCompleted 回调 → JiangnanBattleGame 发 BattleEndEvent(PlayerVictory)
///
/// 与 Foundation 边界：本类 = 唯一持有 Engine.TimeScale / Camera2D / InputEvent 的写入方；
/// Foundation 内 Director 走自己的 TimeScaleController/CameraRequestBus/CombatCinematicLock 抽象,
/// 平台细节由本 adapter 桥接。
/// </summary>
public sealed partial class DecisiveStrikeGodotAdapter : Node
{
    private BattleEventBus _eventBus = null!;
    private TimeScaleController _timeScale = null!;
    private CameraRequestBus _cameraBus = null!;
    private CombatCinematicLock _cinematicLock = null!;
    private CombatAnimationDirector _director = null!;

    private TimeScaleEngineBridge _timeScaleBridge = null!;
    private CameraRequestBusBridge _cameraBridge = null!;

    private CanvasLayer _overlay = null!;
    private Label _phaseLabel = null!;
    private Label _stylePlaceholder = null!;
    private Label _damageLabel = null!;

    private readonly Dictionary<string, Func<Vector2>> _targetPositionProviders = new(StringComparer.Ordinal);

    private int _pendingDamage;
    private bool _initialized;
    private bool _disposed;

    /// <summary>演出结束（自然 7-phase 走完）回调。</summary>
    public Action? SequenceCompleted;

    /// <summary>演出被取消（CancelCurrent / Dispose 中断）回调。</summary>
    public Action? SequenceCancelled;

    /// <summary>当前是否在演出中（director busy）。</summary>
    public bool IsRunning => _initialized && _director.IsBusy;

    /// <summary>
    /// 初始化。在 JiangnanBattleGame._Ready() 创建 adapter 节点并挂到 scene tree 后调用。
    /// </summary>
    public void Initialize(BattleEventBus eventBus, Camera2D camera, CanvasLayer overlay)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(overlay);

        _eventBus = eventBus;
        _overlay = overlay;
        _phaseLabel = overlay.GetNode<Label>("CenterContainer/VBox/PhaseLabel");
        _stylePlaceholder = overlay.GetNode<Label>("CenterContainer/VBox/StylePlaceholder");
        _damageLabel = overlay.GetNode<Label>("CenterContainer/VBox/DamageLabel");
        _overlay.Visible = false;
        _damageLabel.Visible = false;

        _timeScale = new TimeScaleController();
        _cameraBus = new CameraRequestBus();
        _cinematicLock = new CombatCinematicLock();

        _director = new CombatAnimationDirector(_eventBus, _timeScale, _cameraBus, _cinematicLock);

        _timeScaleBridge = new TimeScaleEngineBridge(_timeScale);
        _cameraBridge = new CameraRequestBusBridge(_cameraBus, camera, ResolveTargetPosition);

        _eventBus.Subscribe<DecisiveStrikeStartedEvent>(OnStarted);
        _eventBus.Subscribe<DecisiveStrikePhaseAdvancedEvent>(OnPhaseAdvanced);
        _eventBus.Subscribe<DecisiveStrikeCompletedEvent>(OnCompleted);

        _initialized = true;
    }

    /// <summary>
    /// 注册一个角色 id 到世界坐标的 provider。CameraRequestBusBridge 会用它把
    /// CameraRequest.TargetId 解析成 Camera2D.GlobalPosition。
    /// </summary>
    public void RegisterTarget(string id, Func<Vector2> positionProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(positionProvider);
        _targetPositionProviders[id] = positionProvider;
    }

    /// <summary>
    /// 触发决胜演出。已在 IsRunning 状态时拒绝并返回 false（同时只允许一条决胜）。
    /// </summary>
    public bool RequestDecisive(string sourceId, string targetId, int precomputedDamage, MoveType moveType)
    {
        if (!_initialized)
            throw new InvalidOperationException("DecisiveStrikeGodotAdapter.Initialize() not called");
        if (_director.IsBusy)
            return false;

        _pendingDamage = precomputedDamage;
        _overlay.Visible = true;
        _damageLabel.Visible = false;
        _phaseLabel.Text = $"决胜一击 · 准备";
        _stylePlaceholder.Text = $"—— 体系动画 placeholder ({moveType}) ——";

        var req = new DecisiveStrikeRequest(sourceId, targetId, precomputedDamage, moveType);
        _director.RequestDecisiveStrike(req);
        return true;
    }

    /// <summary>
    /// cu-006 AC-2 输入屏蔽：宿主 _UnhandledInput 顶部调用，命中即 SetInputAsHandled + 提前 return。
    /// 默认 whitelist = { "ui_pause", "ui_system_back" }。
    /// </summary>
    public bool ShouldConsumeInput(InputEvent evt)
    {
        if (!_initialized) return false;
        return CombatCinematicLockInputFilter.ShouldConsume(evt, _cinematicLock);
    }

    public override void _Process(double delta)
    {
        if (!_initialized || !_director.IsBusy)
            return;
        // 注意：传入 delta 而非 wall-clock。Foundation Sequence 内部 _paused 时不累计；
        // Engine.TimeScale = 0.2 时 Godot 给我们的 delta 已经是 0.2x 真实时间，sequence 推进自然变慢。
        _director.Tick(delta);
    }

    private Vector2? ResolveTargetPosition(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _targetPositionProviders.TryGetValue(id, out var provider) ? provider() : null;
    }

    private void OnStarted(DecisiveStrikeStartedEvent evt)
    {
        _pendingDamage = evt.PrecomputedDamage;
        _phaseLabel.Text = $"决胜一击 · 开始 → {evt.TargetId}";
    }

    private void OnPhaseAdvanced(DecisiveStrikePhaseAdvancedEvent evt)
    {
        var phase = (DecisiveStrikePhase)evt.PhaseIndex;
        _phaseLabel.Text = $"决胜一击 · Phase {evt.PhaseIndex} {phase}";

        if (phase == DecisiveStrikePhase.Phase5_DamageNumber)
        {
            _damageLabel.Text = _pendingDamage.ToString();
            _damageLabel.Visible = true;
        }
    }

    private void OnCompleted(DecisiveStrikeCompletedEvent evt)
    {
        _overlay.Visible = false;
        _damageLabel.Visible = false;

        if (evt.WasCancelled)
            SequenceCancelled?.Invoke();
        else
            SequenceCompleted?.Invoke();
    }

    public override void _ExitTree()
    {
        if (_disposed)
        {
            base._ExitTree();
            return;
        }
        _disposed = true;

        if (_initialized)
        {
            _eventBus.Unsubscribe<DecisiveStrikeStartedEvent>(OnStarted);
            _eventBus.Unsubscribe<DecisiveStrikePhaseAdvancedEvent>(OnPhaseAdvanced);
            _eventBus.Unsubscribe<DecisiveStrikeCompletedEvent>(OnCompleted);

            _director.Dispose();
            _timeScaleBridge.Dispose();
            _cameraBridge.Dispose();
        }

        base._ExitTree();
    }
}
