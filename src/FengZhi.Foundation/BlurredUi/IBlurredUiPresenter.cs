namespace FengZhi.Foundation.BlurredUi;

public interface IBlurredUiPresenter
{
    void ShowRealmBreakthrough(string realmName, bool isMultiBreakthrough);
    void ShowRealmLabel(string realmName);
    void ShowRelativeStrength(string text);
    void ApplyTintColor(TintColor color, float intensity, float transitionDuration);
    void ClearTint(float transitionDuration);
    void ShowInnerMonologue(string text);
    void ShowEnvironmentNarrative(string text);
    void TriggerForceBreakCutscene(string npcId);
    void HideAllBlurredComponents();
    void RestoreBlurredComponents();
}
