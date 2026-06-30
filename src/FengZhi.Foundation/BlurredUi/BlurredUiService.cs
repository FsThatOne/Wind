namespace FengZhi.Foundation.BlurredUi;

public sealed class BlurredUiService
{
    private readonly RealmChannel _realmChannel;
    private readonly MindsetTintChannel _mindsetChannel;
    private readonly RelationshipChannel _relationshipChannel;
    private readonly RevealQueue _revealQueue;
    private readonly IBlurredUiPresenter _presenter;

    private BlurredUiState _state = BlurredUiState.Blurred;
    private UiContext _activeContext = UiContext.Exploration;
    private bool _frozen;
    private string _playerCharacterId = "player";

    public const float RevealIntervalSeconds = 0.5f;
    public const float TintTransitionDuration = 2.0f;

    public BlurredUiService(
        IRealmDataProvider realmProvider,
        IMindsetDataProvider mindsetProvider,
        IRomanceDataProvider romanceProvider,
        IBlurredUiPresenter presenter,
        int mergeThreshold = RevealQueue.DefaultMergeThreshold)
    {
        _realmChannel = new RealmChannel(realmProvider);
        _mindsetChannel = new MindsetTintChannel(mindsetProvider);
        _relationshipChannel = new RelationshipChannel(romanceProvider);
        _revealQueue = new RevealQueue(mergeThreshold);
        _presenter = presenter;
    }

    public BlurredUiState State => _state;
    public UiContext ActiveContext => _activeContext;
    public int PendingRevealCount => _revealQueue.Count;

    public void Initialize(string playerCharacterId)
    {
        _playerCharacterId = playerCharacterId;
        _realmChannel.Initialize(playerCharacterId);
    }

    public void OnCombatEnter()
    {
        _frozen = true;
        _state = BlurredUiState.TransitionToClear;
        _presenter.HideAllBlurredComponents();
        _state = BlurredUiState.Clear;
    }

    public void OnCombatExit()
    {
        _state = BlurredUiState.TransitionToBlurred;
        _frozen = false;
        _presenter.RestoreBlurredComponents();

        var tint = _mindsetChannel.GetCurrentTint();
        float intensity = _mindsetChannel.GetCurrentIntensity();
        _presenter.ApplyTintColor(tint, intensity, TintTransitionDuration);

        _state = BlurredUiState.Blurred;
    }

    public void SetContext(UiContext context)
    {
        _activeContext = context;

        if (_state != BlurredUiState.Blurred || _frozen) return;

        ProcessPendingReveals();
    }

    public void Tick(long timestamp)
    {
        if (_state != BlurredUiState.Blurred || _frozen) return;

        var realmReveal = _realmChannel.CheckForBreakthrough(_playerCharacterId, timestamp);
        if (realmReveal != null)
            _revealQueue.Enqueue(realmReveal);

        var mindsetReveal = _mindsetChannel.CheckForZoneChange(timestamp);
        if (mindsetReveal != null)
            _revealQueue.Enqueue(mindsetReveal);

        MergeIfNeeded(RevealChannel.Realm);
        MergeIfNeeded(RevealChannel.Mindset);
        MergeIfNeeded(RevealChannel.Relationship);
    }

    public void OnAttitudeChanged(string npcId, long timestamp)
    {
        if (_state != BlurredUiState.Blurred) return;

        var reveal = _relationshipChannel.CheckForTierChange(npcId, timestamp);
        if (reveal != null)
            _revealQueue.Enqueue(reveal);

        MergeIfNeeded(RevealChannel.Relationship);
    }

    public void OnForceBreak(string npcId, long timestamp)
    {
        var reveal = _relationshipChannel.CreateForceBreakReveal(npcId, timestamp);
        _presenter.TriggerForceBreakCutscene(npcId);
        _presenter.ShowEnvironmentNarrative(reveal.Message);
    }

    public void OnPanelOpened()
    {
        SetContext(UiContext.Panel);

        if (_state != BlurredUiState.Blurred) return;

        string realmName = _realmChannel.GetCurrentRealmDisplay(_playerCharacterId);
        _presenter.ShowRealmLabel(realmName);

        var realmReveal = _revealQueue.DequeueByChannel(RevealChannel.Realm);
        if (realmReveal?.Payload is RealmRevealPayload payload)
        {
            _presenter.ShowRealmBreakthrough(payload.RealmName, payload.IsMultiBreakthrough);
        }
    }

    public void OnComparisonRequested(string targetId)
    {
        SetContext(UiContext.Comparison);
        string text = _realmChannel.GetRelativeStrengthText(_playerCharacterId, targetId);
        _presenter.ShowRelativeStrength(text);
    }

    public void OnSceneEntered()
    {
        SetContext(UiContext.Exploration);

        if (_state != BlurredUiState.Blurred) return;

        var mindsetReveal = _revealQueue.DequeueByChannel(RevealChannel.Mindset);
        if (mindsetReveal?.Payload is MindsetRevealPayload tintPayload)
        {
            float intensity = _mindsetChannel.GetCurrentIntensity();
            _presenter.ApplyTintColor(tintPayload.TargetTint, intensity, TintTransitionDuration);
        }

        var echoes = _mindsetChannel.GetPendingEchoes();
        if (echoes.Count > 0)
        {
            _presenter.ShowInnerMonologue(echoes[0]);
            _mindsetChannel.ConsumeEchoes(1);
        }
    }

    public void OnRelatedNpcEvent(string npcId)
    {
        if (_state != BlurredUiState.Blurred) return;

        var reveal = _revealQueue.DequeueByChannel(RevealChannel.Relationship);
        if (reveal != null)
        {
            _presenter.ShowEnvironmentNarrative(reveal.Message);
        }

        var cometEvents = _relationshipChannel.GetPendingCometEvents();
        if (cometEvents.Count > 0)
        {
            _presenter.ShowEnvironmentNarrative(cometEvents[0].Description);
            _relationshipChannel.ConsumeCometEvent(cometEvents[0].Id);
        }
    }

    public BlurredUiSaveData GetSaveData()
    {
        return new BlurredUiSaveData
        {
            PendingReveals = _revealQueue.ExportAll(),
            LastKnownRealmTier = _realmChannel.LastKnownTier,
            LastKnownMindsetZone = _mindsetChannel.LastKnownZone
        };
    }

    public void LoadSaveData(BlurredUiSaveData data)
    {
        _revealQueue.ImportAll(data.PendingReveals);
        _realmChannel.SetLastKnownTier(data.LastKnownRealmTier);
        _mindsetChannel.SetLastKnownZone(data.LastKnownMindsetZone);
    }

    private void ProcessPendingReveals()
    {
        foreach (RevealChannel ch in Enum.GetValues<RevealChannel>())
        {
            var reveal = _revealQueue.PeekByChannel(ch);
            if (reveal == null) continue;

            if (ShouldRevealInContext(ch, _activeContext))
            {
                _revealQueue.DequeueByChannel(ch);
                DispatchReveal(reveal);
            }
        }
    }

    private void DispatchReveal(PendingReveal reveal)
    {
        switch (reveal.Channel)
        {
            case RevealChannel.Realm when reveal.Payload is RealmRevealPayload rp:
                _presenter.ShowRealmBreakthrough(rp.RealmName, rp.IsMultiBreakthrough);
                break;
            case RevealChannel.Mindset when reveal.Payload is MindsetRevealPayload mp:
                float intensity = _mindsetChannel.GetCurrentIntensity();
                _presenter.ApplyTintColor(mp.TargetTint, intensity, TintTransitionDuration);
                break;
            case RevealChannel.Relationship:
                _presenter.ShowEnvironmentNarrative(reveal.Message);
                break;
        }
    }

    private void MergeIfNeeded(RevealChannel channel)
    {
        if (!_revealQueue.NeedsMerge(channel)) return;

        var items = _revealQueue.PendingReveals
            .Where(r => r.Channel == channel)
            .ToList();

        PendingReveal merged = channel switch
        {
            RevealChannel.Realm => RealmChannel.CreateMergedReveal(items[^1]),
            RevealChannel.Mindset => MindsetTintChannel.CreateMergedReveal(items[^1]),
            RevealChannel.Relationship => RelationshipChannel.CreateMergedReveal(items),
            _ => items[^1]
        };

        _revealQueue.MergeChannel(channel, merged);
    }

    private static bool ShouldRevealInContext(RevealChannel channel, UiContext context)
    {
        return channel switch
        {
            RevealChannel.Realm => context is UiContext.Panel or UiContext.CombatResult,
            RevealChannel.Mindset => context is UiContext.Exploration,
            RevealChannel.Relationship => context is UiContext.Exploration or UiContext.Dialogue,
            _ => false
        };
    }
}
