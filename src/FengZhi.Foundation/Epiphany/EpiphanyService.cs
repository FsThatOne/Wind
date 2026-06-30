namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanyService
{
    private readonly EpiphanyRegistry _registry;
    private readonly CombatEpiphanyChecker _checker;
    private readonly IEpiphanyFlagProvider _flags;
    private readonly IEpiphanyRewardTarget _rewardTarget;

    private string _activeEpiphanyId = string.Empty;
    private string _activeCharacterId = string.Empty;
    private int _focusTurnsRemaining;

    public const int DefaultFocusTurns = 3;
    public const float PostEpiphanyBuffMultiplier = 1.10f;

    public EpiphanyService(
        EpiphanyRegistry registry,
        CombatEpiphanyChecker checker,
        IEpiphanyFlagProvider flags,
        IEpiphanyRewardTarget rewardTarget)
    {
        _registry = registry;
        _checker = checker;
        _flags = flags;
        _rewardTarget = rewardTarget;
    }

    public string ActiveEpiphanyId => _activeEpiphanyId;
    public int FocusTurnsRemaining => _focusTurnsRemaining;
    public bool IsInFocus => _focusTurnsRemaining > 0;

    public void RefreshAvailability()
    {
        _registry.UpdateAvailability(_flags);
    }

    public EpiphanyConfig? CheckCombatTrigger(
        string characterId,
        IEpiphanyCombatContext context,
        Random? rng = null)
    {
        int chapter = _flags.GetCurrentChapter();
        if (_registry.IsChapterCapReached(chapter)) return null;

        var config = _registry.FindBestAvailableCombatEpiphany(chapter);
        if (config?.CombatCondition == null) return null;

        if (!_checker.MeetsCombatConditions(config.CombatCondition, context, characterId))
            return null;

        var instance = _registry.GetInstance(config.Id);
        if (instance == null) return null;

        float hpRatio = context.GetCurrentHpRatio(characterId);
        float chance = _checker.ComputeChance(hpRatio, instance.SkipCount);
        if (!_checker.Roll(chance, rng)) return null;

        instance.State = EpiphanyEventState.Triggered;
        _activeEpiphanyId = config.Id;
        _activeCharacterId = characterId;
        context.MarkEpiphanyTriggered();
        return config;
    }

    public EpiphanyConfig? TriggerNarrativeEpiphany(string nodeId, string characterId)
    {
        var config = _registry.FindNarrativeEpiphany(nodeId);
        if (config == null) return null;

        var instance = _registry.GetInstance(config.Id)!;
        instance.State = EpiphanyEventState.Completed;
        _flags.SetFlag($"{config.Id}_done");

        int chapter = _flags.GetCurrentChapter();
        _registry.ConsumeChapterQuota(chapter, config.ChapterCapCost);
        DispatchReward(config, characterId);
        return config;
    }

    public EpiphanyConfig? TriggerMeditationEpiphany(string characterId)
    {
        var config = _registry.FindMeditationEpiphany(_flags);
        if (config == null) return null;

        var instance = _registry.GetInstance(config.Id)!;
        instance.State = EpiphanyEventState.Completed;
        _flags.SetFlag($"{config.Id}_done");

        int chapter = _flags.GetCurrentChapter();
        _registry.ConsumeChapterQuota(chapter, config.ChapterCapCost);
        _registry.IncrementMeditationCount();
        DispatchReward(config, characterId);
        return config;
    }

    public void OnPlayerChoose(EpiphanyChoice choice, IEpiphanyCombatContext context)
    {
        if (string.IsNullOrEmpty(_activeEpiphanyId)) return;

        var config = _registry.GetConfig(_activeEpiphanyId);
        var instance = _registry.GetInstance(_activeEpiphanyId);
        if (config == null || instance == null) return;

        if (choice == EpiphanyChoice.Focus)
        {
            instance.State = EpiphanyEventState.Focusing;
            _focusTurnsRemaining = DefaultFocusTurns;
        }
        else
        {
            instance.State = EpiphanyEventState.Skipped;
            instance.SkipCount++;

            if (instance.SkipCount >= config.SkipLimit)
                instance.State = EpiphanyEventState.Exhausted;
            else
                instance.State = EpiphanyEventState.Available;

            _rewardTarget.ApplySkipReward(_activeCharacterId, config.SkipRewardConfig);
            context.ResumeCombat();
            _activeEpiphanyId = string.Empty;
        }
    }

    public void OnFocusTurnPassed()
    {
        if (_focusTurnsRemaining <= 0) return;

        _focusTurnsRemaining--;

        if (_focusTurnsRemaining <= 0)
        {
            CompleteFocusEpiphany();
        }
    }

    public void OnFocusCharacterDied()
    {
        if (string.IsNullOrEmpty(_activeEpiphanyId)) return;

        var config = _registry.GetConfig(_activeEpiphanyId);
        var instance = _registry.GetInstance(_activeEpiphanyId);
        if (instance == null) return;

        if (config?.DeathProtection == true) return;

        instance.State = EpiphanyEventState.Failed;
        _focusTurnsRemaining = 0;
        _activeEpiphanyId = string.Empty;
    }

    public void OnBattleEnded()
    {
        if (string.IsNullOrEmpty(_activeEpiphanyId)) return;

        var instance = _registry.GetInstance(_activeEpiphanyId);
        if (instance?.State == EpiphanyEventState.Failed ||
            instance?.State == EpiphanyEventState.Focusing)
        {
            instance.State = EpiphanyEventState.Available;
        }

        _focusTurnsRemaining = 0;
        _activeEpiphanyId = string.Empty;
    }

    private void CompleteFocusEpiphany()
    {
        var config = _registry.GetConfig(_activeEpiphanyId);
        var instance = _registry.GetInstance(_activeEpiphanyId);
        if (config == null || instance == null) return;

        instance.State = EpiphanyEventState.Completed;
        _flags.SetFlag($"{config.Id}_done");

        int chapter = _flags.GetCurrentChapter();
        _registry.ConsumeChapterQuota(chapter, config.ChapterCapCost);

        DispatchReward(config, _activeCharacterId);
        _rewardTarget.ApplyPostEpiphanyBuff(_activeCharacterId, PostEpiphanyBuffMultiplier);
        _rewardTarget.RefreshCooldowns(_activeCharacterId);
        _activeEpiphanyId = string.Empty;
    }

    private void DispatchReward(EpiphanyConfig config, string characterId)
    {
        if (config.Reward.Attributes.Count > 0)
            _rewardTarget.ApplyPermanentAttributeGrowth(characterId, config.Reward.Attributes);

        if (!string.IsNullOrEmpty(config.Reward.UnlockMoveId))
            _rewardTarget.UnlockMove(characterId, config.Reward.UnlockMoveId);

        if (!string.IsNullOrEmpty(config.Reward.UnlockNarrativeOption))
            _rewardTarget.UnlockNarrativeOption(config.Reward.UnlockNarrativeOption);

        if (!string.IsNullOrEmpty(config.Reward.UnlockAbility))
            _rewardTarget.UnlockAbility(characterId, config.Reward.UnlockAbility);

        _rewardTarget.CheckRealmBreakthrough(characterId);
    }
}
