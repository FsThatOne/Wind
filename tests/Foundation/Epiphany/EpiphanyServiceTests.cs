using FengZhi.Foundation.Epiphany;
using Xunit;

namespace Foundation.Tests.Epiphany;

public class EpiphanyServiceTests
{
    private class FakeFlags : IEpiphanyFlagProvider
    {
        public HashSet<string> Flags { get; } = new();
        public int Chapter { get; set; } = 1;
        public int MeditationDays { get; set; }

        public bool HasFlag(string flag) => Flags.Contains(flag);
        public void SetFlag(string flag) => Flags.Add(flag);
        public int GetCurrentChapter() => Chapter;
        public int GetMeditationDays() => MeditationDays;
    }

    private class FakeRewardTarget : IEpiphanyRewardTarget
    {
        public Dictionary<string, int>? LastAttributes { get; set; }
        public string? LastMoveUnlocked { get; set; }
        public string? LastAbilityUnlocked { get; set; }
        public string? LastNarrativeOption { get; set; }
        public float LastBuffMultiplier { get; set; }
        public bool CooldownsRefreshed { get; set; }
        public SkipReward? LastSkipReward { get; set; }
        public bool BreakthroughChecked { get; set; }

        public void ApplyPermanentAttributeGrowth(string cid, Dictionary<string, int> attrs) => LastAttributes = attrs;
        public void UnlockMove(string cid, string moveId) => LastMoveUnlocked = moveId;
        public void UnlockNarrativeOption(string optionId) => LastNarrativeOption = optionId;
        public void UnlockAbility(string cid, string abilityId) => LastAbilityUnlocked = abilityId;
        public void ApplyPostEpiphanyBuff(string cid, float mult) => LastBuffMultiplier = mult;
        public void RefreshCooldowns(string cid) => CooldownsRefreshed = true;
        public void ApplySkipReward(string cid, SkipReward reward) => LastSkipReward = reward;
        public void CheckRealmBreakthrough(string cid) => BreakthroughChecked = true;
    }

    private static EpiphanyConfig CreateCombatConfig(string id = "ep_test", int priority = 70)
    {
        return new EpiphanyConfig
        {
            Id = id,
            Type = EpiphanyType.Combat,
            ChapterMin = 1,
            ChapterMax = 6,
            Priority = priority,
            CombatCondition = new CombatTriggerCondition { HpThreshold = 0.2f, MinActorActions = 5 },
            Reward = new EpiphanyReward
            {
                Attributes = new() { ["strength"] = 2, ["agility"] = 1 },
                UnlockMoveId = "breaking_water"
            }
        };
    }

    private (EpiphanyService svc, FakeFlags flags, FakeRewardTarget rewards, EpiphanyRegistry registry) Create()
    {
        var flags = new FakeFlags { Chapter = 1 };
        var rewards = new FakeRewardTarget();
        var registry = new EpiphanyRegistry();
        var checker = new CombatEpiphanyChecker();
        var svc = new EpiphanyService(registry, checker, flags, rewards);
        return (svc, flags, rewards, registry);
    }

    [Fact]
    public void NarrativeEpiphany_CompletesImmediately()
    {
        var (svc, flags, rewards, registry) = Create();
        var config = new EpiphanyConfig
        {
            Id = "ep_narrative",
            Type = EpiphanyType.Narrative,
            ChapterMin = 1, ChapterMax = 3,
            NarrativeNodeId = "ch1_revelation",
            Reward = new EpiphanyReward { Attributes = new() { ["insight"] = 2 } }
        };
        registry.RegisterConfig(config);
        svc.RefreshAvailability();

        var result = svc.TriggerNarrativeEpiphany("ch1_revelation", "player");

        Assert.NotNull(result);
        Assert.True(flags.HasFlag("ep_narrative_done"));
        Assert.Equal(2, rewards.LastAttributes!["insight"]);
        Assert.True(rewards.BreakthroughChecked);
    }

    [Fact]
    public void NarrativeEpiphany_IgnoresChapterCap()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.SetChapterCap(1, 0);
        var config = new EpiphanyConfig
        {
            Id = "ep_nar2",
            Type = EpiphanyType.Narrative,
            ChapterMin = 1, ChapterMax = 3,
            NarrativeNodeId = "node_x",
            Reward = new EpiphanyReward { Attributes = new() { ["constitution"] = 1 } }
        };
        registry.RegisterConfig(config);
        svc.RefreshAvailability();

        var result = svc.TriggerNarrativeEpiphany("node_x", "player");
        Assert.NotNull(result);
    }

    [Fact]
    public void MeditationEpiphany_RequiresDaysAndFlags()
    {
        var (svc, flags, rewards, registry) = Create();
        var config = new EpiphanyConfig
        {
            Id = "ep_meditate",
            Type = EpiphanyType.Meditation,
            ChapterMin = 1, ChapterMax = 6,
            MeditationCondition = new MeditationCondition
            {
                RequiredFlags = new() { "visited_cave" },
                MinMeditationDays = 3
            },
            Reward = new EpiphanyReward { Attributes = new() { ["inner_power"] = 2 } }
        };
        registry.RegisterConfig(config);

        flags.MeditationDays = 2;
        flags.Flags.Add("visited_cave");
        svc.RefreshAvailability();
        Assert.Null(svc.TriggerMeditationEpiphany("player"));

        flags.MeditationDays = 3;
        var result = svc.TriggerMeditationEpiphany("player");
        Assert.NotNull(result);
        Assert.Equal(1, registry.MeditationEpiphanyCount);
    }

    [Fact]
    public void CombatTrigger_ChapterCapReached_ReturnsNull()
    {
        var (svc, flags, _, registry) = Create();
        registry.SetChapterCap(1, 0);
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.1f, ActorActions = 10 };
        var rng = new Random(1);
        Assert.Null(svc.CheckCombatTrigger("player", context, rng));
    }

    [Fact]
    public void CombatTrigger_ConditionsMet_Triggers()
    {
        var (svc, flags, _, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        var rng = new Random(0);

        EpiphanyConfig? result = null;
        for (int i = 0; i < 20; i++)
        {
            context.AlreadyTriggered = false;
            result = svc.CheckCombatTrigger("player", context, new Random(i));
            if (result != null) break;
        }
        Assert.NotNull(result);
    }

    [Fact]
    public void OnPlayerChoose_Skip_AppliesSkipReward()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        for (int i = 0; i < 100; i++)
        {
            context.AlreadyTriggered = false;
            if (svc.CheckCombatTrigger("player", context, new Random(i)) != null) break;
        }

        svc.OnPlayerChoose(EpiphanyChoice.Skip, context);
        Assert.NotNull(rewards.LastSkipReward);
        Assert.Equal(string.Empty, svc.ActiveEpiphanyId);
    }

    [Fact]
    public void OnPlayerChoose_Focus_EntersFocusing()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        for (int i = 0; i < 100; i++)
        {
            context.AlreadyTriggered = false;
            if (svc.CheckCombatTrigger("player", context, new Random(i)) != null) break;
        }

        svc.OnPlayerChoose(EpiphanyChoice.Focus, context);
        Assert.Equal(3, svc.FocusTurnsRemaining);
        Assert.True(svc.IsInFocus);
    }

    [Fact]
    public void FocusTurns_CompletesAfterThree_DispatchesReward()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        for (int i = 0; i < 100; i++)
        {
            context.AlreadyTriggered = false;
            if (svc.CheckCombatTrigger("player", context, new Random(i)) != null) break;
        }
        svc.OnPlayerChoose(EpiphanyChoice.Focus, context);

        svc.OnFocusTurnPassed();
        svc.OnFocusTurnPassed();
        Assert.True(svc.IsInFocus);

        svc.OnFocusTurnPassed();
        Assert.False(svc.IsInFocus);
        Assert.Equal(2, rewards.LastAttributes!["strength"]);
        Assert.Equal("breaking_water", rewards.LastMoveUnlocked);
        Assert.Equal(1.10f, rewards.LastBuffMultiplier, 2);
        Assert.True(rewards.CooldownsRefreshed);
    }

    [Fact]
    public void FocusCharacterDied_FailsEpiphany()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig());
        svc.RefreshAvailability();

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        for (int i = 0; i < 100; i++)
        {
            context.AlreadyTriggered = false;
            if (svc.CheckCombatTrigger("player", context, new Random(i)) != null) break;
        }
        svc.OnPlayerChoose(EpiphanyChoice.Focus, context);

        svc.OnFocusCharacterDied();
        Assert.False(svc.IsInFocus);
        Assert.Null(rewards.LastAttributes);
    }

    [Fact]
    public void SkipLimit_ExhaustsEvent()
    {
        var (svc, flags, rewards, registry) = Create();
        var config = CreateCombatConfig();
        registry.RegisterConfig(config);
        svc.RefreshAvailability();

        var inst = registry.GetInstance(config.Id)!;
        inst.SkipCount = 2;

        var context = new FakeCombatContext { HpRatio = 0.05f, ActorActions = 10 };
        for (int i = 0; i < 100; i++)
        {
            context.AlreadyTriggered = false;
            if (svc.CheckCombatTrigger("player", context, new Random(i)) != null) break;
        }

        svc.OnPlayerChoose(EpiphanyChoice.Skip, context);
        Assert.Equal(EpiphanyEventState.Exhausted, inst.State);
    }

    [Fact]
    public void SaveAndLoad_PreservesState()
    {
        var (svc, flags, rewards, registry) = Create();
        registry.RegisterConfig(CreateCombatConfig("ep1"));
        registry.RegisterConfig(new EpiphanyConfig
        {
            Id = "ep2", Type = EpiphanyType.Narrative,
            ChapterMin = 1, ChapterMax = 6, NarrativeNodeId = "n1",
            Reward = new EpiphanyReward()
        });
        svc.RefreshAvailability();
        svc.TriggerNarrativeEpiphany("n1", "player");

        var saveData = registry.ExportState();
        Assert.Equal(EpiphanyEventState.Completed, saveData.EventStates["ep2"].State);
        Assert.Equal(1, saveData.ChapterUsed[1]);

        var newRegistry = new EpiphanyRegistry();
        newRegistry.RegisterConfig(CreateCombatConfig("ep1"));
        newRegistry.RegisterConfig(new EpiphanyConfig
        {
            Id = "ep2", Type = EpiphanyType.Narrative,
            ChapterMin = 1, ChapterMax = 6, NarrativeNodeId = "n1",
            Reward = new EpiphanyReward()
        });
        newRegistry.LoadState(saveData);

        Assert.Equal(EpiphanyEventState.Completed, newRegistry.GetInstance("ep2")!.State);
    }
}
