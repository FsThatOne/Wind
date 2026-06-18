using Godot;
using Sprint5CombatUiHarness.TestData;
using Sprint5CombatUiHarness.UI;

namespace Sprint5CombatUiHarness;

public partial class Sprint5CombatUiHarness : Node
{
    public override void _Ready()
    {
        var view = new Sprint5CombatUiHarnessView();
        AddChild(view);
        view.Initialize(Sprint5CombatUiFixtures.All);
    }
}
