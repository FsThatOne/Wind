using System.Collections.Generic;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Xunit;

namespace FengZhi.Tests.Foundation;

/// <summary>
/// S7-VS-Outcome-Feedback Subtask 6 · 验证战斗结算心境位移接线层逻辑。
///
/// 关注点：spec §3 choice → shifts 映射 + 真实 MindsetService 状态变化 +
/// EventBus 事件广播 + Snapshot 契约。
///
/// 不依赖 Godot：测试 Foundation 层的 MindsetOutcomeShifts + MindsetService +
/// EventBus 组合，模拟 GameFlow.ApplyOutcomeChoice 的内部逻辑。
/// </summary>
public class GameFlowMindsetIntegrationTest
{
    private static (MindsetService service, EventBus bus, List<MindsetShiftedEvent> shifts,
        List<MindsetZoneChangedEvent> zoneChanges, List<MoralityTierChangedEvent> tierChanges)
        BuildHarness(MindsetState? initial = null)
    {
        var bus = new EventBus();
        var service = new MindsetService(initial ?? MindsetState.CreateDefault(), bus);
        var shifts = new List<MindsetShiftedEvent>();
        var zoneChanges = new List<MindsetZoneChangedEvent>();
        var tierChanges = new List<MoralityTierChangedEvent>();
        bus.Subscribe<MindsetShiftedEvent>(shifts.Add);
        bus.Subscribe<MindsetZoneChangedEvent>(zoneChanges.Add);
        bus.Subscribe<MoralityTierChangedEvent>(tierChanges.Add);
        return (service, bus, shifts, zoneChanges, tierChanges);
    }

    [Fact]
    public void Spare_Applies_Morality_Plus5_And_Resolve_Plus2_With_Two_Shift_Events()
    {
        var (service, _, shifts, _, _) = BuildHarness();
        var oldState = service.State;

        service.ApplyShifts(MindsetOutcomeShifts.ResolveShifts(MindsetOutcomeChoice.Spare));

        Assert.Equal(oldState.Morality + 5, service.State.Morality);
        Assert.Equal(oldState.Resolve + 2, service.State.Resolve);
        Assert.Equal(oldState.Worldly, service.State.Worldly);

        Assert.Equal(2, shifts.Count);
        Assert.Equal(MindsetAxis.Morality, shifts[0].Axis);
        Assert.Equal(+5, shifts[0].Delta);
        Assert.Equal(MindsetAxis.Resolve, shifts[1].Axis);
        Assert.Equal(+2, shifts[1].Delta);
    }

    [Fact]
    public void Defeat_Applies_Morality_Minus5_And_Resolve_Minus2_With_Two_Shift_Events()
    {
        var (service, _, shifts, _, _) = BuildHarness();
        var oldState = service.State;

        service.ApplyShifts(MindsetOutcomeShifts.ResolveShifts(MindsetOutcomeChoice.Defeat));

        Assert.Equal(oldState.Morality - 5, service.State.Morality);
        Assert.Equal(oldState.Resolve - 2, service.State.Resolve);
        Assert.Equal(oldState.Worldly, service.State.Worldly);

        Assert.Equal(2, shifts.Count);
        Assert.Equal(MindsetAxis.Morality, shifts[0].Axis);
        Assert.Equal(-5, shifts[0].Delta);
        Assert.Equal(MindsetAxis.Resolve, shifts[1].Axis);
        Assert.Equal(-2, shifts[1].Delta);
    }

    [Fact]
    public void Spare_Crossing_Resolve_PositiveZone_Boundary_Fires_ZoneChanged()
    {
        var nearPositiveResolve = MindsetState.FromValues(
            resolve: 14, worldly: 0, morality: 0, reputation: 0);
        var (service, _, _, zoneChanges, _) = BuildHarness(nearPositiveResolve);

        service.ApplyShifts(MindsetOutcomeShifts.ResolveShifts(MindsetOutcomeChoice.Spare));

        Assert.Equal(16, service.State.Resolve);
        Assert.Single(zoneChanges);
        Assert.Equal(MindsetZone.ZhongYong, zoneChanges[0].OldZone);
        Assert.Equal(MindsetZone.ShiHuaiWeiDing, zoneChanges[0].NewZone);
    }

    [Fact]
    public void Snapshot_Contract_Records_Old_And_New_State_Plus_Applied_Shifts()
    {
        var (service, _, _, _, _) = BuildHarness();
        var oldState = service.State;
        var oldZone = service.CurrentZone;
        var oldTier = service.CurrentMoralityTier;

        var shifts = MindsetOutcomeShifts.ResolveShifts(MindsetOutcomeChoice.Spare);
        service.ApplyShifts(shifts);

        var snapshot = new MindsetShiftSnapshotData(
            OldState: oldState,
            NewState: service.State,
            OldZone: oldZone,
            NewZone: service.CurrentZone,
            OldMoralityTier: oldTier,
            NewMoralityTier: service.CurrentMoralityTier,
            AppliedShifts: shifts);

        Assert.NotEqual(snapshot.OldState, snapshot.NewState);
        Assert.Equal(oldState.Morality + 5, snapshot.NewState.Morality);
        Assert.Equal(oldState.Resolve + 2, snapshot.NewState.Resolve);
        Assert.Equal(2, snapshot.AppliedShifts.Count);
        Assert.Equal(MindsetAxis.Morality, snapshot.AppliedShifts[0].Axis);
        Assert.Equal(MindsetAxis.Resolve, snapshot.AppliedShifts[1].Axis);
    }

    private sealed record MindsetShiftSnapshotData(
        MindsetState OldState,
        MindsetState NewState,
        MindsetZone OldZone,
        MindsetZone NewZone,
        MoralityTier OldMoralityTier,
        MoralityTier NewMoralityTier,
        IReadOnlyList<MindsetShift> AppliedShifts);
}
