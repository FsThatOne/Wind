namespace FengZhi.Foundation.Cutscene;

public interface ICutscenePlayable
{
    CutsceneScript CurrentScript { get; }
    bool HasNext { get; }
    void Advance();
    int TransitionGapMs { get; }
}

public sealed class SingleCutsceneRequest : ICutscenePlayable
{
    public CutsceneScript CurrentScript { get; }
    public bool HasNext => false;
    public int TransitionGapMs => 0;
    public void Advance() { }

    public SingleCutsceneRequest(CutsceneScript script)
    {
        CurrentScript = script;
    }
}

public sealed class CutsceneChainRequest : ICutscenePlayable
{
    private readonly CutsceneScript[] _scripts;
    private int _currentIndex;

    public int TransitionGapMs { get; }
    public CutsceneScript CurrentScript => _scripts[_currentIndex];
    public bool HasNext => _currentIndex < _scripts.Length - 1;

    public void Advance()
    {
        if (HasNext) _currentIndex++;
    }

    public CutsceneChainRequest(CutsceneScript[] scripts, int transitionGapMs = 500)
    {
        if (scripts.Length == 0)
            throw new ArgumentException("Chain must have at least one script.");
        _scripts = scripts;
        TransitionGapMs = transitionGapMs;
    }
}
