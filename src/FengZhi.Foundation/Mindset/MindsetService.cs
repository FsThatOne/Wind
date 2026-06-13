using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Mindset;

public sealed class MindsetService
{
    private readonly IEventBus? _eventBus;

    public MindsetService(MindsetState? state = null, IEventBus? eventBus = null)
    {
        State = state ?? MindsetState.CreateDefault();
        _eventBus = eventBus;
    }

    public MindsetState State { get; private set; }

    public MindsetZone CurrentZone =>
        MindsetMath.CombineZone(State.CurrentResolveZone!.Value, State.CurrentWorldlyZone!.Value);

    public MoralityTier CurrentMoralityTier => MindsetMath.GetMoralityTier(State.Morality);

    public MoralityTier CurrentReputationTier => MindsetMath.GetMoralityTier(State.Reputation);

    public string GetResolveDescription()
    {
        return MindsetPresentationService.GetResolveDescription(State);
    }

    public string GetWorldlyDescription()
    {
        return MindsetPresentationService.GetWorldlyDescription(State);
    }

    public MindsetVisualParams GetVisualParams()
    {
        return MindsetPresentationService.GetVisualParams(CurrentZone);
    }

    public void ApplyShift(MindsetAxis axis, int delta)
    {
        ApplyShifts(new[] { new MindsetShift(axis, delta) });
    }

    public void ApplyShifts(IEnumerable<MindsetShift> shifts)
    {
        var oldZone = CurrentZone;
        var oldMoralityTier = CurrentMoralityTier;

        foreach (var shift in shifts)
        {
            ApplyShiftValue(shift.Axis, shift.Delta);
        }

        UpdateZones();
        PublishZoneChange(oldZone, CurrentZone);
        PublishMoralityTierChange(oldMoralityTier, CurrentMoralityTier);
    }

    public bool MindsetCheck(MindsetAxis axis, MindsetComparison comparison, int threshold)
    {
        var value = axis switch
        {
            MindsetAxis.Resolve => State.Resolve,
            MindsetAxis.Worldly => State.Worldly,
            MindsetAxis.Morality => State.Morality,
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        return comparison switch
        {
            MindsetComparison.LessThan => value < threshold,
            MindsetComparison.LessThanOrEqual => value <= threshold,
            MindsetComparison.Equal => value == threshold,
            MindsetComparison.GreaterThanOrEqual => value >= threshold,
            MindsetComparison.GreaterThan => value > threshold,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
        };
    }

    public EndingResult DetermineEnding()
    {
        return DetermineEnding(State.Resolve, State.Worldly, State.Morality);
    }

    public void UpdateReputation(float catchUpRate = MindsetConstants.DefaultReputationCatchUpRate)
    {
        var oldTier = CurrentReputationTier;
        var next = State.Reputation + (State.Morality - State.Reputation) * catchUpRate;
        var rounded = (int)MathF.Round(next, MidpointRounding.AwayFromZero);

        State = State with { Reputation = MindsetMath.ClampAxis(rounded) };

        var newTier = CurrentReputationTier;
        if (oldTier != newTier)
            _eventBus?.Publish(new ReputationTierChangedEvent(oldTier, newTier));
    }

    public void ApplyMindsetBattleResult(IEnumerable<MindsetBattleAttempt> attempts)
    {
        var finalAttempt = attempts.LastOrDefault(attempt => attempt.IsFinalNarrativeResult)
            ?? attempts.LastOrDefault();

        if (finalAttempt != null)
            ApplyShifts(finalAttempt.Shifts);
    }

    public static EndingResult DetermineEnding(int resolve, int worldly, int morality)
    {
        var moralityTier = MindsetMath.GetMoralityTier(morality);
        if (moralityTier == MoralityTier.ExtremeEvil)
            return new EndingResult(BaseEnding.MoDao, moralityTier);

        var resolveZone = MindsetMath.GetInitialPolarity(resolve);
        var worldlyZone = MindsetMath.GetInitialPolarity(worldly);
        var baseEnding = DetermineBaseEnding(resolve, worldly, resolveZone, worldlyZone);

        return new EndingResult(baseEnding, moralityTier);
    }

    private static BaseEnding DetermineBaseEnding(
        int resolve,
        int worldly,
        AxisPolarity resolveZone,
        AxisPolarity worldlyZone)
    {
        if (resolveZone == AxisPolarity.Negative && worldlyZone == AxisPolarity.Negative)
            return BaseEnding.GuJianZhanShi;
        if (resolveZone == AxisPolarity.Negative && worldlyZone == AxisPolarity.Positive)
            return BaseEnding.FengZhiJianMing;
        if (resolveZone == AxisPolarity.Positive && worldlyZone == AxisPolarity.Negative)
            return BaseEnding.BaiYiXingTian;
        if (resolveZone == AxisPolarity.Positive && worldlyZone == AxisPolarity.Positive)
            return BaseEnding.DaYinYuShi;

        var absResolve = Math.Abs(resolve);
        var absWorldly = Math.Abs(worldly);

        if (absResolve > absWorldly)
        {
            if (resolve < 0)
                return worldly <= 0 ? BaseEnding.GuJianZhanShi : BaseEnding.FengZhiJianMing;
            return worldly <= 0 ? BaseEnding.BaiYiXingTian : BaseEnding.DaYinYuShi;
        }

        if (absWorldly > absResolve)
        {
            if (worldly < 0)
                return resolve <= 0 ? BaseEnding.GuJianZhanShi : BaseEnding.BaiYiXingTian;
            return resolve <= 0 ? BaseEnding.FengZhiJianMing : BaseEnding.DaYinYuShi;
        }

        return BaseEnding.Undecided;
    }

    private void ApplyShiftValue(MindsetAxis axis, int delta)
    {
        var oldValue = axis switch
        {
            MindsetAxis.Resolve => State.Resolve,
            MindsetAxis.Worldly => State.Worldly,
            MindsetAxis.Morality => State.Morality,
            _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        var newValue = MindsetMath.ClampAxis(oldValue + delta);
        State = axis switch
        {
            MindsetAxis.Resolve => State with { Resolve = newValue },
            MindsetAxis.Worldly => State with { Worldly = newValue },
            MindsetAxis.Morality => State with { Morality = newValue },
            _ => State
        };

        _eventBus?.Publish(new MindsetShiftedEvent(axis, oldValue, newValue, delta));
    }

    private void UpdateZones()
    {
        State = State with
        {
            CurrentResolveZone = MindsetMath.ApplyHysteresis(State.Resolve, State.CurrentResolveZone!.Value),
            CurrentWorldlyZone = MindsetMath.ApplyHysteresis(State.Worldly, State.CurrentWorldlyZone!.Value)
        };
    }

    private void PublishZoneChange(MindsetZone oldZone, MindsetZone newZone)
    {
        if (oldZone != newZone)
            _eventBus?.Publish(new MindsetZoneChangedEvent(oldZone, newZone));
    }

    private void PublishMoralityTierChange(MoralityTier oldTier, MoralityTier newTier)
    {
        if (oldTier != newTier)
            _eventBus?.Publish(new MoralityTierChangedEvent(oldTier, newTier));
    }
}

public sealed record MindsetShift(MindsetAxis Axis, int Delta);

public sealed record MindsetBattleAttempt(
    IReadOnlyList<MindsetShift> Shifts,
    bool IsFinalNarrativeResult);

public sealed class MindsetCompatibilityService
{
    private readonly IReadOnlyDictionary<string, IReadOnlySet<MindsetZone>> _compatibleZonesByNpcId;

    public MindsetCompatibilityService(IReadOnlyDictionary<string, IReadOnlySet<MindsetZone>> compatibleZonesByNpcId)
    {
        _compatibleZonesByNpcId = compatibleZonesByNpcId;
    }

    public bool IsZoneCompatible(string npcId, MindsetZone currentZone)
    {
        if (!_compatibleZonesByNpcId.TryGetValue(npcId, out var compatibleZones))
            return true;

        return compatibleZones.Count == 0 || compatibleZones.Contains(currentZone);
    }
}

public sealed record MindsetLoadResult(MindsetState State, IReadOnlyList<string> Warnings);

public static class MindsetSaveLoader
{
    public static MindsetLoadResult Load(
        int resolve,
        int worldly,
        int morality,
        int reputation,
        AxisPolarity? currentResolveZone = null,
        AxisPolarity? currentWorldlyZone = null)
    {
        var warnings = new List<string>();
        AddRangeWarning(warnings, nameof(resolve), resolve);
        AddRangeWarning(warnings, nameof(worldly), worldly);
        AddRangeWarning(warnings, nameof(morality), morality);
        AddRangeWarning(warnings, nameof(reputation), reputation);

        return new MindsetLoadResult(
            MindsetState.FromValues(resolve, worldly, morality, reputation, currentResolveZone, currentWorldlyZone),
            warnings);
    }

    private static void AddRangeWarning(ICollection<string> warnings, string field, int value)
    {
        if (value < MindsetConstants.MinAxisValue || value > MindsetConstants.MaxAxisValue)
            warnings.Add($"{field}={value} 超出 [{MindsetConstants.MinAxisValue}, {MindsetConstants.MaxAxisValue}]，已钳位。");
    }
}

public enum MindsetComparison
{
    LessThan,
    LessThanOrEqual,
    Equal,
    GreaterThanOrEqual,
    GreaterThan
}
