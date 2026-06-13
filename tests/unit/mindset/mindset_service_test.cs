using FengZhi.Foundation.Events;
using FengZhi.Foundation.Mindset;
using Xunit;

namespace FengZhi.Tests.Foundation.Mindset;

public class MindsetServiceTest
{
    [Fact]
    public void CreateDefault_InitializesCoordinatesAndDerivedState()
    {
        var service = new MindsetService();

        Assert.Equal(-5, service.State.Resolve);
        Assert.Equal(0, service.State.Worldly);
        Assert.Equal(0, service.State.Morality);
        Assert.Equal(0, service.State.Reputation);
        Assert.Equal(AxisPolarity.Neutral, service.State.CurrentResolveZone);
        Assert.Equal(AxisPolarity.Neutral, service.State.CurrentWorldlyZone);
        Assert.Equal(MindsetZone.ZhongYong, service.CurrentZone);
        Assert.Equal(MoralityTier.Neutral, service.CurrentMoralityTier);
    }

    [Fact]
    public void ApplyShift_UpdatesOnlyTargetAxis()
    {
        var service = new MindsetService();

        service.ApplyShift(MindsetAxis.Resolve, 8);

        Assert.Equal(3, service.State.Resolve);
        Assert.Equal(0, service.State.Worldly);
        Assert.Equal(0, service.State.Morality);
    }

    [Fact]
    public void ApplyShift_ClampsAxisToRange()
    {
        var service = new MindsetService(MindsetState.FromValues(0, 0, 48, 0));

        service.ApplyShift(MindsetAxis.Morality, 5);

        Assert.Equal(50, service.State.Morality);
    }

    [Fact]
    public void ApplyShift_NeutralToPositive_ChangesZoneAndPublishesEvent()
    {
        var bus = new EventBus();
        MindsetZoneChangedEvent? received = null;
        bus.Subscribe<MindsetZoneChangedEvent>(e => received = e);
        var service = new MindsetService(MindsetState.FromValues(14, 0, 0, 0), bus);

        service.ApplyShift(MindsetAxis.Resolve, 2);

        Assert.Equal(AxisPolarity.Positive, service.State.CurrentResolveZone);
        Assert.Equal(MindsetZone.ShiHuaiWeiDing, service.CurrentZone);
        Assert.NotNull(received);
        Assert.Equal(MindsetZone.ZhongYong, received.OldZone);
        Assert.Equal(MindsetZone.ShiHuaiWeiDing, received.NewZone);
    }

    [Fact]
    public void ApplyShift_PositiveInsideHysteresis_KeepsPositiveZone()
    {
        var service = new MindsetService(
            MindsetState.FromValues(15, 0, 0, 0, AxisPolarity.Positive, AxisPolarity.Neutral));

        service.ApplyShift(MindsetAxis.Resolve, -1);

        Assert.Equal(14, service.State.Resolve);
        Assert.Equal(AxisPolarity.Positive, service.State.CurrentResolveZone);
    }

    [Fact]
    public void ApplyShift_PositivePastHysteresis_ExitsToNeutral()
    {
        var bus = new EventBus();
        MindsetZoneChangedEvent? received = null;
        bus.Subscribe<MindsetZoneChangedEvent>(e => received = e);
        var service = new MindsetService(
            MindsetState.FromValues(14, 0, 0, 0, AxisPolarity.Positive, AxisPolarity.Neutral),
            bus);

        service.ApplyShift(MindsetAxis.Resolve, -2);

        Assert.Equal(12, service.State.Resolve);
        Assert.Equal(AxisPolarity.Neutral, service.State.CurrentResolveZone);
        Assert.NotNull(received);
        Assert.Equal(MindsetZone.ShiHuaiWeiDing, received.OldZone);
        Assert.Equal(MindsetZone.ZhongYong, received.NewZone);
    }

    [Fact]
    public void ApplyShift_MoralityTierCrossesBoundary_PublishesTierEvent()
    {
        var bus = new EventBus();
        MoralityTierChangedEvent? received = null;
        bus.Subscribe<MoralityTierChangedEvent>(e => received = e);
        var service = new MindsetService(MindsetState.FromValues(0, 0, -16, 0), bus);

        service.ApplyShift(MindsetAxis.Morality, 2);

        Assert.Equal(-14, service.State.Morality);
        Assert.Equal(MoralityTier.Neutral, service.CurrentMoralityTier);
        Assert.NotNull(received);
        Assert.Equal(MoralityTier.Evil, received.OldTier);
        Assert.Equal(MoralityTier.Neutral, received.NewTier);
    }

    [Theory]
    [InlineData(20, -25, BaseEnding.BaiYiXingTian)]
    [InlineData(10, 8, BaseEnding.DaYinYuShi)]
    [InlineData(5, 5, BaseEnding.Undecided)]
    public void DetermineEnding_UsesSnapshotZoneAndNeutralTieRules(int resolve, int worldly, BaseEnding expected)
    {
        var ending = MindsetService.DetermineEnding(resolve, worldly, 0);

        Assert.Equal(expected, ending.BaseEnding);
    }

    [Fact]
    public void DetermineEnding_IncludesMoralityDecoration()
    {
        var ending = MindsetService.DetermineEnding(20, 20, 32);

        Assert.Equal(BaseEnding.DaYinYuShi, ending.BaseEnding);
        Assert.Equal(MoralityTier.ExtremeGood, ending.MoralityTier);
    }

    [Fact]
    public void DetermineEnding_ExtremeEvilOverridesBaseEndingToMoDao()
    {
        var ending = MindsetService.DetermineEnding(20, 20, -30);

        Assert.Equal(BaseEnding.MoDao, ending.BaseEnding);
        Assert.Equal(MoralityTier.ExtremeEvil, ending.MoralityTier);
    }

    [Fact]
    public void TurningPoint_CanMoveEvilToNeutral()
    {
        var service = new MindsetService(MindsetState.FromValues(0, 0, -25, 0));

        service.ApplyShift(MindsetAxis.Morality, 12);

        Assert.Equal(-13, service.State.Morality);
        Assert.Equal(MoralityTier.Neutral, service.CurrentMoralityTier);
    }

    [Fact]
    public void RedemptionPath_CanMoveEvilToGood()
    {
        var service = new MindsetService(MindsetState.FromValues(0, 0, -25, 0));

        for (var i = 0; i < 15; i++)
            service.ApplyShift(MindsetAxis.Morality, 2);
        service.ApplyShift(MindsetAxis.Morality, 12);

        Assert.Equal(17, service.State.Morality);
        Assert.Equal(MoralityTier.Good, service.CurrentMoralityTier);
    }

    [Fact]
    public void IsZoneCompatible_ReturnsFalseWhenCurrentZoneNotAllowed()
    {
        var compatibility = new MindsetCompatibilityService(
            new Dictionary<string, IReadOnlySet<MindsetZone>>
            {
                ["companion"] = new HashSet<MindsetZone>
                {
                    MindsetZone.BaiYiRuShi,
                    MindsetZone.DaYinYuShi
                }
            });

        var result = compatibility.IsZoneCompatible("companion", MindsetZone.GuJianRuShi);

        Assert.False(result);
    }

    [Fact]
    public void IsZoneCompatible_EmptyCompatibilityListAllowsAnyZone()
    {
        var compatibility = new MindsetCompatibilityService(
            new Dictionary<string, IReadOnlySet<MindsetZone>>
            {
                ["companion"] = new HashSet<MindsetZone>()
            });

        Assert.True(compatibility.IsZoneCompatible("companion", MindsetZone.GuJianRuShi));
    }

    [Fact]
    public void ApplyMindsetBattleResult_AppliesOnlyFinalNarrativeAttempt()
    {
        var service = new MindsetService(MindsetState.FromValues(0, 0, 0, 0));

        service.ApplyMindsetBattleResult(new[]
        {
            new MindsetBattleAttempt(new[] { new MindsetShift(MindsetAxis.Resolve, -5) }, false),
            new MindsetBattleAttempt(new[] { new MindsetShift(MindsetAxis.Resolve, -3) }, true)
        });

        Assert.Equal(-3, service.State.Resolve);
    }

    [Fact]
    public void ApplyShifts_BatchesZoneCheckAfterAllShifts()
    {
        var bus = new EventBus();
        var zoneEvents = new List<MindsetZoneChangedEvent>();
        bus.Subscribe<MindsetZoneChangedEvent>(zoneEvents.Add);
        var service = new MindsetService(eventBus: bus);

        service.ApplyShifts(new[]
        {
            new MindsetShift(MindsetAxis.Resolve, 5),
            new MindsetShift(MindsetAxis.Morality, 3)
        });

        Assert.Equal(0, service.State.Resolve);
        Assert.Equal(3, service.State.Morality);
        Assert.Empty(zoneEvents);
    }

    [Theory]
    [InlineData(-20, -10, -12)]
    [InlineData(5, -20, -15)]
    public void UpdateReputation_CatchesUpTowardMorality(int morality, int reputation, int expected)
    {
        var service = new MindsetService(MindsetState.FromValues(0, 0, morality, reputation));

        service.UpdateReputation();

        Assert.Equal(expected, service.State.Reputation);
    }

    [Fact]
    public void FromValues_ClampsLoadedSaveAndRecomputesMissingZones()
    {
        var loadResult = MindsetSaveLoader.Load(60, 0, 0, 0);

        Assert.Equal(50, loadResult.State.Resolve);
        Assert.Equal(AxisPolarity.Positive, loadResult.State.CurrentResolveZone);
        Assert.Single(loadResult.Warnings);
        Assert.Contains("resolve=60", loadResult.Warnings[0]);
    }

    [Fact]
    public void FromValues_UsesInitialZoneForLegacySaveWithoutZoneFields()
    {
        var state = MindsetState.FromValues(17, 0, 0, 0);

        Assert.Equal(AxisPolarity.Positive, state.CurrentResolveZone);
    }

    [Fact]
    public void MindsetCheck_EvaluatesAxisAgainstThreshold()
    {
        var service = new MindsetService(MindsetState.FromValues(20, -10, 5, 0));

        Assert.True(service.MindsetCheck(MindsetAxis.Resolve, MindsetComparison.GreaterThanOrEqual, 15));
        Assert.True(service.MindsetCheck(MindsetAxis.Worldly, MindsetComparison.LessThan, 0));
        Assert.False(service.MindsetCheck(MindsetAxis.Morality, MindsetComparison.Equal, 0));
    }

    [Fact]
    public void PresentationQueries_AreAvailableThroughMindsetService()
    {
        var service = new MindsetService(MindsetState.FromValues(20, 20, 0, 0));

        Assert.Contains("往事", service.GetResolveDescription());
        Assert.Contains("山水", service.GetWorldlyDescription());
        Assert.Equal("warm-muted", service.GetVisualParams().Tone);
    }
}
