using Xunit;
using FengZhi.Foundation.Cutscene;

namespace Foundation.Tests.Cutscene;

public class CutsceneServiceTests
{
    private readonly MockPresenter _presenter = new();
    private readonly MockStateLock _lock = new();
    private readonly MockEffectExecutor _executor = new();
    private readonly MockViewedStore _viewedStore = new();

    private CutsceneService CreateService() =>
        new(_presenter, _lock, _executor, _viewedStore);

    [Fact]
    public void PlayCutscene_StartsImmediately_WhenIdle()
    {
        var svc = CreateService();
        var script = new CutsceneScript
        {
            Id = "immediate",
            LockMode = LockMode.Full,
            Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } }
        };

        svc.PlayCutscene(script);
        Assert.True(svc.IsPlaying);
        Assert.Equal("immediate", svc.CurrentScript?.Id);
    }

    [Fact]
    public void PlayCutscene_Queues_WhenBusy()
    {
        var svc = CreateService();
        var s1 = new CutsceneScript { Id = "s1", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 2.0f } } };
        var s2 = new CutsceneScript { Id = "s2", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } } };

        svc.PlayCutscene(s1);
        svc.PlayCutscene(s2);

        Assert.Equal("s1", svc.CurrentScript?.Id);
        Assert.Equal(1, svc.QueueCount);
    }

    [Fact]
    public void AutoPlays_NextInQueue_AfterCompletion()
    {
        var svc = CreateService();
        var s1 = new CutsceneScript { Id = "s1", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } } };
        var s2 = new CutsceneScript { Id = "s2", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } } };

        svc.PlayCutscene(s1);
        svc.PlayCutscene(s2);

        svc.Tick(1.0f); // s1 completes
        Assert.Equal("s2", svc.CurrentScript?.Id);
    }

    [Fact]
    public void Chain_PlaysSequentially_WithGap()
    {
        var svc = CreateService();
        var scripts = new[]
        {
            new CutsceneScript { Id = "ch1", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 0.5f } } },
            new CutsceneScript { Id = "ch2", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 0.5f } } },
        };

        string? lastCompleted = null;
        svc.CutsceneCompleted += id => lastCompleted = id;

        svc.PlayCutsceneChain(scripts, 200);

        svc.Tick(0.5f); // ch1 完成
        Assert.Equal("ch1", lastCompleted);

        // 进入过渡间隔
        Assert.True(svc.IsPlaying);
        svc.Tick(0.1f); // 100ms, gap还没结束
        Assert.True(svc.IsPlaying);

        svc.Tick(0.15f); // 超过 200ms gap
        Assert.Equal("ch2", svc.CurrentScript?.Id);
    }

    [Fact]
    public void AllCompleted_Fires_WhenQueueEmpty()
    {
        var svc = CreateService();
        bool allDone = false;
        svc.AllCompleted += () => allDone = true;

        var script = new CutsceneScript { Id = "only", LockMode = LockMode.None, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 0.5f } } };

        svc.PlayCutscene(script);
        svc.Tick(0.5f);

        Assert.True(allDone);
    }

    [Fact]
    public void CutsceneCompleted_Event_Fires_PerScript()
    {
        var svc = CreateService();
        var completed = new List<string>();
        svc.CutsceneCompleted += id => completed.Add(id);

        var s1 = new CutsceneScript { Id = "a", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } } };
        var s2 = new CutsceneScript { Id = "b", LockMode = LockMode.Full, Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 1.0f } } };

        svc.PlayCutscene(s1);
        svc.PlayCutscene(s2);

        svc.Tick(1.0f);
        svc.Tick(1.0f);

        Assert.Equal(new[] { "a", "b" }, completed);
    }

    [Fact]
    public void Skip_DuringPlay_StillExecutesEffects()
    {
        var svc = CreateService();
        var script = new CutsceneScript
        {
            Id = "skip_test",
            Skippable = true,
            LockMode = LockMode.Full,
            Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 10.0f } },
            OnComplete = new[] { new GameplayEffect { Type = EffectType.SetFlag, Key = "done" } }
        };

        svc.PlayCutscene(script);
        svc.OnSkipHeld(1.0f);

        Assert.Single(_executor.Executed);
        Assert.Equal("done", _executor.Executed[0].Key);
    }

    // --- Mocks ---

    private class MockPresenter : ICutscenePresenter
    {
        public void ShowImage(string imageId, TransitionType transition, float duration) { }
        public void ShowText(string text, TextStyle style, float duration) { }
        public void PlayAnimation(string animId, string? target, bool loop) { }
        public void StopAnimation(string animId) { }
        public void MoveCamera(float zoom, EasingType easing, float duration) { }
        public void SetTimeScale(float scale) { }
        public void RestoreTimeScale() { }
        public void ApplyScreenEffect(ScreenEffectType effect, float duration) { }
        public void PlaySfx(string sfxId, float volume) { }
        public void PlayBgm(string bgmId, int fadeInMs) { }
        public void RestoreBgm(int fadeOutMs) { }
        public void ShowSkipProgress(float progress) { }
        public void HideSkipProgress() { }
        public void FadeToBlack(float duration) { }
        public void FadeFromBlack(float duration) { }
        public void HideAllHud() { }
        public void RestoreHud() { }
        public void ShowPlaceholder(string scriptId) { }
    }

    private class MockStateLock : IGameStateLock
    {
        public void Acquire(LockMode mode) { }
        public void Release() { }
        public bool IsLocked(LockMode mode) => false;
    }

    private class MockEffectExecutor : ICutsceneEffectExecutor
    {
        public List<GameplayEffect> Executed { get; } = new();
        public void Execute(GameplayEffect effect) => Executed.Add(effect);
    }

    private class MockViewedStore : ICutsceneViewedStore
    {
        private readonly HashSet<string> _viewed = new();
        public bool HasViewed(string scriptId) => _viewed.Contains(scriptId);
        public void MarkViewed(string scriptId) => _viewed.Add(scriptId);
        public HashSet<string> GetAllViewed() => new(_viewed);
        public void RestoreViewed(HashSet<string> viewedIds) { _viewed.Clear(); foreach (var id in viewedIds) _viewed.Add(id); }
    }
}
