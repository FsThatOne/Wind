namespace FengZhi.Foundation.Settings;

public interface ISettingsPersistence
{
    SettingsData? Load();
    void Save(SettingsData data);
}

public interface ISettingsApplier
{
    void ApplyVolume(string track, int value);
    void ApplyResolution(string resolution);
    void ApplyWindowMode(WindowMode mode);
    void ApplyFontScale(FontScale scale);
    void ApplyTextSpeed(TextSpeed speed);
    void RevertResolution();
}

public interface IPauseMenuPresenter
{
    void ShowMenu(IReadOnlyList<PauseMenuItem> items);
    void HideMenu();
    void ShowConfirmDialog(string message, Action onConfirm, Action onCancel);
    void ShowResolutionCountdown(int secondsRemaining);
    void HideResolutionCountdown();
}

public interface ICinematicLockQuery
{
    bool IsInCinematicLock();
}
