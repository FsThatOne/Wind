// VERTICAL SLICE - Boss Battle Entry Point
using Godot;

namespace FengzhiSlice;

/// <summary>
/// Directly launches the Boss battle without dialogue flow.
/// Attach to root node of BossBattle.tscn.
/// </summary>
public partial class BossBattleEntry : Node
{
    public override void _Ready()
    {
        GD.Print("[BossBattle] === 铁冠道人 Boss 战 Prototype ===");
        GD.Print("[BossBattle] Foundation EnemyBrain 4阶段 AI 已加载");

        // Wire combat after one frame
        Callable.From(StartBossBattle).CallDeferred();
    }

    private void StartBossBattle()
    {
        var manager = GetNodeOrNull<BossCombatManager>("BossCombatManager");
        var ui = GetNodeOrNull<BossBattleUI>("UILayer/BossBattleUI");

        if (manager == null || ui == null)
        {
            GD.PrintErr("[BossBattle] Missing BossCombatManager or BossBattleUI node!");
            return;
        }

        manager.Initialize();
        ui.BindManager(manager);
        manager.BeginCombat();
    }
}
