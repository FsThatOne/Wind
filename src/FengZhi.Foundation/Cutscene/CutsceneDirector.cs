namespace FengZhi.Foundation.Cutscene;

public sealed class CutsceneDirector
{
    private readonly ICutscenePresenter _presenter;
    private readonly IGameStateLock _lock;
    private readonly ICutsceneEffectExecutor _effectExecutor;
    private readonly ICutsceneViewedStore _viewedStore;

    private CutsceneScript? _currentScript;
    private int _currentStepIndex;
    private float _stepElapsed;
    private float _skipHoldTime;
    private bool _waitingForInput;

    public CutsceneState State { get; private set; } = CutsceneState.Idle;
    public CutsceneScript? CurrentScript => _currentScript;
    public int CurrentStepIndex => _currentStepIndex;

    public float SkipHoldThreshold { get; set; } = 1.0f;
    public float SkipFadeoutDuration { get; set; } = 0.3f;

    public event Action<string>? CutsceneCompleted;

    public CutsceneDirector(
        ICutscenePresenter presenter,
        IGameStateLock stateLock,
        ICutsceneEffectExecutor effectExecutor,
        ICutsceneViewedStore viewedStore)
    {
        _presenter = presenter;
        _lock = stateLock;
        _effectExecutor = effectExecutor;
        _viewedStore = viewedStore;
    }

    public void Begin(CutsceneScript script)
    {
        _currentScript = script;
        _currentStepIndex = 0;
        _stepElapsed = 0f;
        _skipHoldTime = 0f;
        _waitingForInput = false;

        State = CutsceneState.Loading;
        _lock.Acquire(script.LockMode);
        _presenter.HideAllHud();

        if (script.BgmOverride != null)
            _presenter.PlayBgm(script.BgmOverride, 500);

        State = CutsceneState.Playing;
        ExecuteCurrentStep();
    }

    public void Tick(float deltaSeconds)
    {
        if (State != CutsceneState.Playing) return;
        if (_currentScript == null) return;
        if (_waitingForInput) return;

        _stepElapsed += deltaSeconds;
        var step = _currentScript.Steps[_currentStepIndex];
        float duration = step.GetEffectiveDuration();

        if (_stepElapsed >= duration)
        {
            AdvanceStep();
        }
    }

    public void OnInputConfirm()
    {
        if (State != CutsceneState.Playing) return;
        if (_waitingForInput)
        {
            _waitingForInput = false;
            AdvanceStep();
        }
    }

    public void OnSkipHeld(float deltaSeconds)
    {
        if (State != CutsceneState.Playing) return;
        if (_currentScript == null) return;

        if (!CanSkip()) return;

        _skipHoldTime += deltaSeconds;
        _presenter.ShowSkipProgress(_skipHoldTime / SkipHoldThreshold);

        if (_skipHoldTime >= SkipHoldThreshold)
        {
            DoSkip();
        }
    }

    public void OnSkipReleased()
    {
        _skipHoldTime = 0f;
        _presenter.HideSkipProgress();
    }

    public void ForceComplete()
    {
        if (State == CutsceneState.Idle || State == CutsceneState.Completed) return;
        DoSkip();
    }

    private bool CanSkip()
    {
        if (_currentScript == null) return false;
        if (!_currentScript.Skippable) return false;
        if (_currentScript.FirstViewUnskippable && !_viewedStore.HasViewed(_currentScript.Id))
            return false;
        return true;
    }

    private void DoSkip()
    {
        State = CutsceneState.Skipping;
        _presenter.HideSkipProgress();
        _presenter.FadeToBlack(SkipFadeoutDuration);

        State = CutsceneState.CompletingEffects;
        ExecuteOnCompleteEffects();
        FinishCutscene();
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;
        _stepElapsed = 0f;

        if (_currentScript == null) return;

        if (_currentStepIndex >= _currentScript.Steps.Count)
        {
            State = CutsceneState.CompletingEffects;
            ExecuteOnCompleteEffects();
            FinishCutscene();
            return;
        }

        ExecuteCurrentStep();
    }

    private void ExecuteCurrentStep()
    {
        if (_currentScript == null) return;
        var step = _currentScript.Steps[_currentStepIndex];

        switch (step.Type)
        {
            case StepType.ShowImage:
                _presenter.ShowImage(step.ImageId ?? "", step.ImageTransition, step.Duration);
                break;
            case StepType.ShowText:
                _presenter.ShowText(step.Text ?? "", step.TextStyle, step.Duration);
                break;
            case StepType.PlayAnimation:
                _presenter.PlayAnimation(step.AnimationId ?? "", step.AnimationTarget, step.Loop);
                break;
            case StepType.CameraMove:
                _presenter.MoveCamera(step.CameraZoom, step.CameraEasing, step.Duration);
                break;
            case StepType.SlowMotion:
                _presenter.SetTimeScale(step.TimeScale);
                break;
            case StepType.ScreenEffect:
                _presenter.ApplyScreenEffect(step.ScreenEffect, step.Duration);
                break;
            case StepType.PlaySfx:
                _presenter.PlaySfx(step.AudioId ?? "", step.Volume);
                break;
            case StepType.PlayBgm:
                _presenter.PlayBgm(step.AudioId ?? "", step.FadeInMs);
                break;
            case StepType.Wait:
                break;
            case StepType.WaitInput:
                _waitingForInput = true;
                break;
            case StepType.Parallel:
                if (step.ParallelSteps != null)
                {
                    foreach (var sub in step.ParallelSteps)
                        ExecuteSubStep(sub);
                }
                break;
        }
    }

    private void ExecuteSubStep(CutsceneStep step)
    {
        switch (step.Type)
        {
            case StepType.ShowImage:
                _presenter.ShowImage(step.ImageId ?? "", step.ImageTransition, step.Duration);
                break;
            case StepType.ShowText:
                _presenter.ShowText(step.Text ?? "", step.TextStyle, step.Duration);
                break;
            case StepType.PlayAnimation:
                _presenter.PlayAnimation(step.AnimationId ?? "", step.AnimationTarget, step.Loop);
                break;
            case StepType.PlaySfx:
                _presenter.PlaySfx(step.AudioId ?? "", step.Volume);
                break;
            case StepType.ScreenEffect:
                _presenter.ApplyScreenEffect(step.ScreenEffect, step.Duration);
                break;
            case StepType.SlowMotion:
                _presenter.SetTimeScale(step.TimeScale);
                break;
            default:
                break;
        }
    }

    private void ExecuteOnCompleteEffects()
    {
        if (_currentScript == null) return;
        foreach (var effect in _currentScript.OnComplete)
        {
            _effectExecutor.Execute(effect);
        }
    }

    private void FinishCutscene()
    {
        if (_currentScript == null) return;

        var scriptId = _currentScript.Id;
        _viewedStore.MarkViewed(scriptId);

        if (_currentScript.BgmOverride != null)
            _presenter.RestoreBgm(500);

        _presenter.RestoreTimeScale();
        _presenter.FadeFromBlack(SkipFadeoutDuration);
        _presenter.RestoreHud();
        _lock.Release();

        State = CutsceneState.Completed;
        _currentScript = null;
        CutsceneCompleted?.Invoke(scriptId);
    }

    public void Reset()
    {
        State = CutsceneState.Idle;
        _currentScript = null;
        _currentStepIndex = 0;
        _stepElapsed = 0f;
        _skipHoldTime = 0f;
        _waitingForInput = false;
    }
}
