using FengZhi.Foundation.Combat;
using Godot;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 战斗 UI 的 Godot 节点入口，负责创建基础 CanvasLayer 并管理事件适配器生命周期。
/// </summary>
public partial class CombatUiRoot : Node
{
    private CombatUiEventAdapter? _adapter;

    public WorldIntentLayer? WorldIntentLayer { get; private set; }

    public CombatHudLayer? HudLayer { get; private set; }

    public CombatUiEventAdapter? Adapter => _adapter;

    /// <summary>
    /// 创建并显示战斗 UI 基础层，同时开始订阅战斗事件。
    /// </summary>
    public CombatUiEventAdapter EnterBattle(BattleEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        if (_adapter is not null)
            return _adapter;

        WorldIntentLayer = new WorldIntentLayer();
        HudLayer = new CombatHudLayer();
        AddChild(WorldIntentLayer);
        AddChild(HudLayer);

        _adapter = new CombatUiEventAdapter(eventBus);
        _adapter.EnterBattle();
        return _adapter;
    }

    public override void _ExitTree()
    {
        _adapter?.Dispose();
        _adapter = null;
        base._ExitTree();
    }
}
