namespace FengZhi.Foundation.Audio;

/// <summary>
/// 音频系统 6 状态枚举。ADR-0009。
/// </summary>
public enum AudioState
{
    Exploration,
    Combat,
    Cutscene,
    Dialogue,
    Menu,
    Silence
}

/// <summary>
/// 音频轨道标识，用于 attenuation 矩阵索引。
/// </summary>
public enum AudioTrack
{
    Bgm,
    Ambient,
    Sfx
}

/// <summary>
/// 音频 FSM 触发器字符串常量。配合 StateMachine&lt;AudioState&gt; 使用。
/// </summary>
public static class AudioTriggers
{
    public const string EnterCombat = "EnterCombat";
    public const string ExitCombat = "ExitCombat";
    public const string StartCutscene = "StartCutscene";
    public const string EndCutscene = "EndCutscene";
    public const string StartDialogue = "StartDialogue";
    public const string EndDialogue = "EndDialogue";
    public const string OpenMenu = "OpenMenu";
    public const string CloseMenu = "CloseMenu";
    public const string ForceSilence = "ForceSilence";
    public const string Resume = "Resume";
}
