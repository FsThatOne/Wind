namespace FengZhi.Foundation.Cutscene;

public sealed class CutsceneService
{
    private readonly CutsceneDirector _director;
    private readonly CutsceneQueue _queue;

    private ICutscenePlayable? _currentPlayable;
    private float _transitionTimer;
    private bool _inTransitionGap;

    public CutsceneState State => _director.State == CutsceneState.Idle && !_inTransitionGap
        ? CutsceneState.Idle
        : _director.State;

    public bool IsPlaying => _director.State == CutsceneState.Playing
                          || _director.State == CutsceneState.Loading
                          || _inTransitionGap;

    public CutsceneScript? CurrentScript => _director.CurrentScript;
    public int QueueCount => _queue.Count;

    public event Action<string>? CutsceneCompleted;
    public event Action? AllCompleted;

    public CutsceneService(
        ICutscenePresenter presenter,
        IGameStateLock stateLock,
        ICutsceneEffectExecutor effectExecutor,
        ICutsceneViewedStore viewedStore)
    {
        _queue = new CutsceneQueue();
        _director = new CutsceneDirector(presenter, stateLock, effectExecutor, viewedStore);
        _director.CutsceneCompleted += OnDirectorCompleted;
    }

    public void PlayCutscene(CutsceneScript script)
    {
        if (_director.State != CutsceneState.Idle && _director.State != CutsceneState.Completed)
        {
            _queue.Enqueue(script);
            _queue.PruneStaleTier4();
            return;
        }

        _currentPlayable = new SingleCutsceneRequest(script);
        _director.Reset();
        _director.Begin(script);
    }

    public void PlayCutsceneChain(CutsceneScript[] scripts, int transitionGapMs = 500)
    {
        var chain = new CutsceneChainRequest(scripts, transitionGapMs);

        if (_director.State != CutsceneState.Idle && _director.State != CutsceneState.Completed)
        {
            _queue.EnqueuePlayable(chain);
            return;
        }

        _currentPlayable = chain;
        _director.Reset();
        _director.Begin(chain.CurrentScript);
    }

    public void Tick(float deltaSeconds)
    {
        if (_inTransitionGap)
        {
            _transitionTimer -= deltaSeconds * 1000f;
            if (_transitionTimer <= 0f)
            {
                _inTransitionGap = false;
                PlayNextInChainOrQueue();
            }
            return;
        }

        _director.Tick(deltaSeconds);
    }

    public void OnInputConfirm() => _director.OnInputConfirm();
    public void OnSkipHeld(float deltaSeconds) => _director.OnSkipHeld(deltaSeconds);
    public void OnSkipReleased() => _director.OnSkipReleased();

    private void OnDirectorCompleted(string scriptId)
    {
        CutsceneCompleted?.Invoke(scriptId);

        if (_currentPlayable != null && _currentPlayable.HasNext)
        {
            _currentPlayable.Advance();
            _inTransitionGap = true;
            _transitionTimer = _currentPlayable.TransitionGapMs;
            return;
        }

        _currentPlayable = null;
        TryPlayNext();
    }

    private void TryPlayNext()
    {
        if (_queue.IsEmpty)
        {
            _director.Reset();
            AllCompleted?.Invoke();
            return;
        }

        _currentPlayable = _queue.Dequeue();
        _director.Reset();
        _director.Begin(_currentPlayable.CurrentScript);
    }

    private void PlayNextInChainOrQueue()
    {
        if (_currentPlayable == null)
        {
            TryPlayNext();
            return;
        }

        _director.Reset();
        _director.Begin(_currentPlayable.CurrentScript);
    }
}
