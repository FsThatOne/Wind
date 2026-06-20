namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// 一击决胜演出调参常量，来源 GDD §Formulas (F2) 与 ADR-0011。
/// 数值改动属于 design tuning，需要伴随 GDD 同步更新。
/// </summary>
public static class DecisiveTuning
{
    public const double TimeScaleMin = 0.2;
    public const double TimeScaleNormal = 1.0;

    public const double SlowInDurationSeconds = 0.3;
    public const double SlowOutDurationSeconds = 0.3;
    public const double CameraPushInDurationSeconds = 0.3;
    public const double CameraRestoreDurationSeconds = 0.3;
    public const double StyleAnimationDurationSeconds = 1.0;
    public const double DamageNumberHoldDurationSeconds = 0.6;

    public const int DecisivePriority = 50;
    public const int PausePriority = 100;
    public const int DefaultPriority = 0;
    public const int CutscenePriority = 80;

    public const float DecisiveCameraZoom = 1.6f;

    public const bool AllowDecisiveSkip = false;

    /// <summary>
    /// TimeScale &lt;= 此阈值视为暂停。Sequence 在此期间不累计 phase 计时。
    /// </summary>
    public const double PauseDetectionThreshold = 0.001;
}
