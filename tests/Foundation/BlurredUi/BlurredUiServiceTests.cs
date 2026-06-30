using FengZhi.Foundation.BlurredUi;
using Xunit;

namespace Foundation.Tests.BlurredUi;

public class BlurredUiServiceTests
{
    private class FakeRealmProvider : IRealmDataProvider
    {
        public int RealmTier { get; set; } = 2;
        public string RealmName { get; set; } = "初窥门径";
        public int RelativeStrength { get; set; } = 0;

        public int GetTotalPower(string characterId) => 40;
        public int GetRealmTier(string characterId) => RealmTier;
        public string GetRealmName(string characterId) => RealmName;
        public int GetRelativeStrength(string selfId, string targetId) => RelativeStrength;
    }

    private class FakeMindsetProvider : IMindsetDataProvider
    {
        public string Zone { get; set; } = "中庸";
        public float Extremity { get; set; } = 0f;
        public List<string> Echoes { get; set; } = new();
        public int ConsumedCount { get; set; }

        public string GetMindsetZone() => Zone;
        public float GetZoneExtremity() => Extremity;
        public IReadOnlyList<string> GetEchoQueue() => Echoes;
        public void ConsumeEcho(int count) => ConsumedCount += count;
    }

    private class FakeRomanceProvider : IRomanceDataProvider
    {
        public int AttitudeTier { get; set; } = 5;
        public string AttitudeTierName { get; set; } = "友善";
        public List<CometEvent> CometEvents { get; set; } = new();
        public List<string> ConsumedEvents { get; set; } = new();

        public int GetAttitudeTier(string npcId) => AttitudeTier;
        public string GetAttitudeTierName(string npcId) => AttitudeTierName;
        public IReadOnlyList<CometEvent> GetPendingCometEvents() => CometEvents;
        public void ConsumeCometEvent(string eventId) => ConsumedEvents.Add(eventId);
    }

    private class FakePresenter : IBlurredUiPresenter
    {
        public string? LastRealmBreakthrough { get; set; }
        public string? LastRealmLabel { get; set; }
        public string? LastRelativeStrength { get; set; }
        public TintColor? LastTint { get; set; }
        public string? LastInnerMonologue { get; set; }
        public string? LastEnvironmentNarrative { get; set; }
        public string? LastForceBreakNpc { get; set; }
        public bool HiddenAll { get; set; }
        public bool RestoredAll { get; set; }

        public void ShowRealmBreakthrough(string realmName, bool isMulti)
            => LastRealmBreakthrough = realmName;
        public void ShowRealmLabel(string realmName)
            => LastRealmLabel = realmName;
        public void ShowRelativeStrength(string text)
            => LastRelativeStrength = text;
        public void ApplyTintColor(TintColor color, float intensity, float duration)
            => LastTint = color;
        public void ClearTint(float duration) => LastTint = null;
        public void ShowInnerMonologue(string text) => LastInnerMonologue = text;
        public void ShowEnvironmentNarrative(string text) => LastEnvironmentNarrative = text;
        public void TriggerForceBreakCutscene(string npcId) => LastForceBreakNpc = npcId;
        public void HideAllBlurredComponents() => HiddenAll = true;
        public void RestoreBlurredComponents() => RestoredAll = true;
    }

    private (BlurredUiService svc, FakeRealmProvider realm, FakeMindsetProvider mindset, FakeRomanceProvider romance, FakePresenter presenter) CreateService()
    {
        var realm = new FakeRealmProvider();
        var mindset = new FakeMindsetProvider();
        var romance = new FakeRomanceProvider();
        var presenter = new FakePresenter();
        var svc = new BlurredUiService(realm, mindset, romance, presenter);
        svc.Initialize("player");
        return (svc, realm, mindset, romance, presenter);
    }

    [Fact]
    public void InitialState_IsBlurred()
    {
        var (svc, _, _, _, _) = CreateService();
        Assert.Equal(BlurredUiState.Blurred, svc.State);
    }

    [Fact]
    public void OnCombatEnter_TransitionsToClear()
    {
        var (svc, _, _, _, presenter) = CreateService();
        svc.OnCombatEnter();

        Assert.Equal(BlurredUiState.Clear, svc.State);
        Assert.True(presenter.HiddenAll);
    }

    [Fact]
    public void OnCombatExit_RestoresBlurred()
    {
        var (svc, _, _, _, presenter) = CreateService();
        svc.OnCombatEnter();
        svc.OnCombatExit();

        Assert.Equal(BlurredUiState.Blurred, svc.State);
        Assert.True(presenter.RestoredAll);
    }

    [Fact]
    public void Tick_DetectsRealmBreakthrough_EnqueuesPending()
    {
        var (svc, realm, _, _, _) = CreateService();

        realm.RealmTier = 3;
        realm.RealmName = "登堂入室";
        svc.Tick(100);

        Assert.Equal(1, svc.PendingRevealCount);
    }

    [Fact]
    public void OnPanelOpened_ShowsRealmLabel()
    {
        var (svc, realm, _, _, presenter) = CreateService();

        realm.RealmName = "初窥门径";
        svc.OnPanelOpened();

        Assert.Equal("初窥门径", presenter.LastRealmLabel);
    }

    [Fact]
    public void OnPanelOpened_DispatchesPendingBreakthrough()
    {
        var (svc, realm, _, _, presenter) = CreateService();

        realm.RealmTier = 4;
        realm.RealmName = "融会贯通";
        svc.Tick(100);
        svc.OnPanelOpened();

        Assert.Equal("融会贯通", presenter.LastRealmBreakthrough);
    }

    [Fact]
    public void OnComparisonRequested_ShowsRelativeStrength()
    {
        var (svc, realm, _, _, presenter) = CreateService();
        realm.RelativeStrength = -2;

        svc.OnComparisonRequested("boss");

        Assert.Equal("此人功力远在你之上", presenter.LastRelativeStrength);
    }

    [Fact]
    public void OnSceneEntered_AppliesMindsetTint()
    {
        var (svc, _, mindset, _, presenter) = CreateService();

        mindset.Zone = "孤剑入世";
        svc.Tick(100);
        svc.OnSceneEntered();

        Assert.NotNull(presenter.LastTint);
        Assert.Equal(0.06f, presenter.LastTint!.Value.R, 3);
    }

    [Fact]
    public void OnSceneEntered_ShowsInnerMonologue()
    {
        var (svc, _, mindset, _, presenter) = CreateService();
        mindset.Echoes.Add("剑意涌动，心绪难平");

        svc.OnSceneEntered();

        Assert.Equal("剑意涌动，心绪难平", presenter.LastInnerMonologue);
    }

    [Fact]
    public void OnForceBreak_ImmediatePresentation()
    {
        var (svc, _, _, _, presenter) = CreateService();

        svc.OnForceBreak("白苓", 500);

        Assert.Equal("白苓", presenter.LastForceBreakNpc);
        Assert.Contains("决裂", presenter.LastEnvironmentNarrative!);
    }

    [Fact]
    public void CombatFreezesPendingReveals()
    {
        var (svc, realm, _, _, presenter) = CreateService();

        realm.RealmTier = 3;
        realm.RealmName = "登堂入室";
        svc.Tick(100);
        svc.OnCombatEnter();

        svc.OnPanelOpened();
        Assert.Null(presenter.LastRealmBreakthrough);

        svc.OnCombatExit();
        svc.OnPanelOpened();
        Assert.Equal("登堂入室", presenter.LastRealmBreakthrough);
    }

    [Fact]
    public void SaveAndLoad_PreservesPendingReveals()
    {
        var (svc, realm, _, _, _) = CreateService();

        realm.RealmTier = 5;
        realm.RealmName = "驾轻就熟";
        svc.Tick(100);

        var saveData = svc.GetSaveData();
        Assert.Equal(1, saveData.PendingReveals.Count);
        Assert.Equal(5, saveData.LastKnownRealmTier);

        var (svc2, _, _, _, presenter2) = CreateService();
        svc2.LoadSaveData(saveData);
        svc2.OnPanelOpened();

        Assert.Equal("驾轻就熟", presenter2.LastRealmBreakthrough);
    }

    [Fact]
    public void MergeThreshold_MergesExcessReveals()
    {
        var realm = new FakeRealmProvider();
        var mindset = new FakeMindsetProvider();
        var romance = new FakeRomanceProvider();
        var presenter = new FakePresenter();
        var svc = new BlurredUiService(realm, mindset, romance, presenter, mergeThreshold: 2);
        svc.Initialize("player");

        realm.RealmTier = 3;
        realm.RealmName = "登堂入室";
        svc.Tick(100);
        realm.RealmTier = 4;
        realm.RealmName = "融会贯通";
        svc.Tick(200);
        realm.RealmTier = 5;
        realm.RealmName = "驾轻就熟";
        svc.Tick(300);

        Assert.Equal(1, svc.PendingRevealCount);
    }
}
