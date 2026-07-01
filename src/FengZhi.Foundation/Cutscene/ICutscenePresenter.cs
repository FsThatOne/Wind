namespace FengZhi.Foundation.Cutscene;

public interface ICutscenePresenter
{
    void ShowImage(string imageId, TransitionType transition, float duration);
    void ShowText(string text, TextStyle style, float duration);
    void PlayAnimation(string animId, string? target, bool loop);
    void StopAnimation(string animId);
    void MoveCamera(float zoom, EasingType easing, float duration);
    void SetTimeScale(float scale);
    void RestoreTimeScale();
    void ApplyScreenEffect(ScreenEffectType effect, float duration);
    void PlaySfx(string sfxId, float volume);
    void PlayBgm(string bgmId, int fadeInMs);
    void RestoreBgm(int fadeOutMs);
    void ShowSkipProgress(float progress);
    void HideSkipProgress();
    void FadeToBlack(float duration);
    void FadeFromBlack(float duration);
    void HideAllHud();
    void RestoreHud();
    void ShowPlaceholder(string scriptId);
}

public interface IGameStateLock
{
    void Acquire(LockMode mode);
    void Release();
    bool IsLocked(LockMode mode);
}

public interface ICutsceneEffectExecutor
{
    void Execute(GameplayEffect effect);
}

public interface ICutsceneViewedStore
{
    bool HasViewed(string scriptId);
    void MarkViewed(string scriptId);
    HashSet<string> GetAllViewed();
    void RestoreViewed(HashSet<string> viewedIds);
}
