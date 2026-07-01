using Xunit;
using FengZhi.Foundation.Cutscene;

namespace Foundation.Tests.Cutscene;

public class CutsceneDirectorTests
{
    private readonly MockPresenter _presenter = new();
    private readonly MockStateLock _lock = new();
    private readonly MockEffectExecutor _executor = new();
    private readonly MockViewedStore _viewedStore = new();

    private CutsceneDirector CreateDirector() =>
        new(_presenter, _lock, _executor, _viewedStore);

    private static CutsceneScript SimpleScript(string id = "test", bool skippable = true) => new()
    {
        Id = id,
        Tier = CutsceneTier.FullScreenPixel,
        Skippable = skippable,
        LockMode = LockMode.Full,
        Steps = new[]
        {
            new CutsceneStep { Type = StepType.Wait, Duration = 1.0f },
            new CutsceneStep { Type = StepType.ShowText, Text = "Hello", TextStyle = TextStyle.Narration, Duration = 2.0f },
        },
        OnComplete = new[]
        {
            new GameplayEffect { Type = EffectType.SetFlag, Key = "test_flag", Value = "true" }
        }
    };

    [Fact]
    public void Begin_TransitionsToPlaying()
    {
        var d = CreateDirector();
        d.Begin(SimpleScript());

        Assert.Equal(CutsceneState.Playing, d.State);
        Assert.True(_lock.IsAcquired);
        Assert.True(_presenter.HudHidden);
    }

    [Fact]
    public void Tick_AdvancesThroughSteps()
    {
        var d = CreateDirector();
        d.Begin(SimpleScript());

        Assert.Equal(0, d.CurrentStepIndex);

        d.Tick(1.0f);
        Assert.Equal(1, d.CurrentStepIndex);
        Assert.Equal("Hello", _presenter.LastText);
    }

    [Fact]
    public void Tick_CompletesAfterAllSteps()
    {
        var d = CreateDirector();
        string? completedId = null;
        d.CutsceneCompleted += id => completedId = id;

        d.Begin(SimpleScript());
        d.Tick(1.0f); // step 0 done
        d.Tick(2.0f); // step 1 done

        Assert.Equal(CutsceneState.Completed, d.State);
        Assert.Equal("test", completedId);
        Assert.False(_lock.IsAcquired);
    }

    [Fact]
    public void OnComplete_Effects_Executed_On_Normal_Finish()
    {
        var d = CreateDirector();
        d.Begin(SimpleScript());
        d.Tick(1.0f);
        d.Tick(2.0f);

        Assert.Single(_executor.Executed);
        Assert.Equal("test_flag", _executor.Executed[0].Key);
    }

    [Fact]
    public void Skip_ExecutesEffectsAndCompletes()
    {
        var d = CreateDirector();
        string? completedId = null;
        d.CutsceneCompleted += id => completedId = id;

        d.Begin(SimpleScript());
        d.OnSkipHeld(1.0f);

        Assert.Equal(CutsceneState.Completed, d.State);
        Assert.Single(_executor.Executed);
        Assert.Equal("test", completedId);
    }

    [Fact]
    public void Skip_NotAllowed_When_Not_Skippable()
    {
        var d = CreateDirector();
        d.Begin(SimpleScript(skippable: false));
        d.OnSkipHeld(2.0f);

        Assert.Equal(CutsceneState.Playing, d.State);
    }

    [Fact]
    public void Skip_NotAllowed_FirstViewUnskippable()
    {
        var d = CreateDirector();
        var script = new CutsceneScript
        {
            Id = "first_view",
            Skippable = true,
            FirstViewUnskippable = true,
            LockMode = LockMode.Full,
            Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 5.0f } },
        };

        d.Begin(script);
        d.OnSkipHeld(2.0f);

        Assert.Equal(CutsceneState.Playing, d.State);
    }

    [Fact]
    public void Skip_Allowed_SecondView_FirstViewUnskippable()
    {
        _viewedStore.ViewedIds.Add("second_view");
        var d = CreateDirector();
        var script = new CutsceneScript
        {
            Id = "second_view",
            Skippable = true,
            FirstViewUnskippable = true,
            LockMode = LockMode.Full,
            Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 5.0f } },
        };

        d.Begin(script);
        d.OnSkipHeld(1.0f);

        Assert.Equal(CutsceneState.Completed, d.State);
    }

    [Fact]
    public void WaitInput_PausesUntilConfirm()
    {
        var d = CreateDirector();
        var script = new CutsceneScript
        {
            Id = "wait_test",
            LockMode = LockMode.Full,
            Steps = new[]
            {
                new CutsceneStep { Type = StepType.WaitInput, PromptText = "Press to continue" },
                new CutsceneStep { Type = StepType.Wait, Duration = 1.0f },
            }
        };

        d.Begin(script);
        d.Tick(10.0f); // should not advance
        Assert.Equal(0, d.CurrentStepIndex);

        d.OnInputConfirm();
        Assert.Equal(1, d.CurrentStepIndex);
    }

    [Fact]
    public void Parallel_Step_Executes_All_Sub_Steps()
    {
        var d = CreateDirector();
        var script = new CutsceneScript
        {
            Id = "parallel_test",
            LockMode = LockMode.None,
            Steps = new[]
            {
                new CutsceneStep
                {
                    Type = StepType.Parallel,
                    Duration = 2.0f,
                    ParallelSteps = new[]
                    {
                        new CutsceneStep { Type = StepType.PlaySfx, AudioId = "sword", Volume = 0.8f, Duration = 1.0f },
                        new CutsceneStep { Type = StepType.ScreenEffect, ScreenEffect = ScreenEffectType.Shake, Duration = 2.0f },
                    }
                }
            }
        };

        d.Begin(script);
        Assert.Equal("sword", _presenter.LastSfx);
        Assert.Equal(ScreenEffectType.Shake, _presenter.LastEffect);
    }

    [Fact]
    public void BgmOverride_PlaysAndRestores()
    {
        var d = CreateDirector();
        var script = new CutsceneScript
        {
            Id = "bgm_test",
            LockMode = LockMode.None,
            BgmOverride = "dramatic_theme",
            Steps = new[] { new CutsceneStep { Type = StepType.Wait, Duration = 0.5f } }
        };

        d.Begin(script);
        Assert.Equal("dramatic_theme", _presenter.LastBgm);

        d.Tick(0.5f);
        Assert.True(_presenter.BgmRestored);
    }

    [Fact]
    public void MarkViewed_OnCompletion()
    {
        var d = CreateDirector();
        d.Begin(SimpleScript("viewed_test"));
        d.Tick(1.0f);
        d.Tick(2.0f);

        Assert.Contains("viewed_test", _viewedStore.ViewedIds);
    }

    // --- Mock implementations ---

    private class MockPresenter : ICutscenePresenter
    {
        public bool HudHidden { get; private set; }
        public string? LastText { get; private set; }
        public string? LastSfx { get; private set; }
        public string? LastBgm { get; private set; }
        public bool BgmRestored { get; private set; }
        public ScreenEffectType? LastEffect { get; private set; }

        public void ShowImage(string imageId, TransitionType transition, float duration) { }
        public void ShowText(string text, TextStyle style, float duration) => LastText = text;
        public void PlayAnimation(string animId, string? target, bool loop) { }
        public void StopAnimation(string animId) { }
        public void MoveCamera(float zoom, EasingType easing, float duration) { }
        public void SetTimeScale(float scale) { }
        public void RestoreTimeScale() { }
        public void ApplyScreenEffect(ScreenEffectType effect, float duration) => LastEffect = effect;
        public void PlaySfx(string sfxId, float volume) => LastSfx = sfxId;
        public void PlayBgm(string bgmId, int fadeInMs) => LastBgm = bgmId;
        public void RestoreBgm(int fadeOutMs) => BgmRestored = true;
        public void ShowSkipProgress(float progress) { }
        public void HideSkipProgress() { }
        public void FadeToBlack(float duration) { }
        public void FadeFromBlack(float duration) { }
        public void HideAllHud() => HudHidden = true;
        public void RestoreHud() => HudHidden = false;
        public void ShowPlaceholder(string scriptId) { }
    }

    private class MockStateLock : IGameStateLock
    {
        public bool IsAcquired { get; private set; }
        public void Acquire(LockMode mode) => IsAcquired = true;
        public void Release() => IsAcquired = false;
        public bool IsLocked(LockMode mode) => IsAcquired;
    }

    private class MockEffectExecutor : ICutsceneEffectExecutor
    {
        public List<GameplayEffect> Executed { get; } = new();
        public void Execute(GameplayEffect effect) => Executed.Add(effect);
    }

    private class MockViewedStore : ICutsceneViewedStore
    {
        public HashSet<string> ViewedIds { get; } = new();
        public bool HasViewed(string scriptId) => ViewedIds.Contains(scriptId);
        public void MarkViewed(string scriptId) => ViewedIds.Add(scriptId);
        public HashSet<string> GetAllViewed() => new(ViewedIds);
        public void RestoreViewed(HashSet<string> viewedIds) { ViewedIds.Clear(); foreach (var id in viewedIds) ViewedIds.Add(id); }
    }
}
