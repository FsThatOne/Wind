namespace FengZhi.Foundation.Audio;

/// <summary>
/// GDD F2 公式：state_attenuation 矩阵。
/// effective_volume(track) = base_volume(track) × GetAttenuation(state, track)
/// </summary>
public static class AudioStateAttenuation
{
    private static readonly float[,] Matrix = new float[6, 3]
    {
        // Exploration: BGM=1.0, Ambient=1.0, SFX=1.0
        { 1.0f, 1.0f, 1.0f },
        // Combat: BGM=1.0, Ambient=0.2, SFX=1.0
        { 1.0f, 0.2f, 1.0f },
        // Cutscene: BGM=1.0(由脚本接管), Ambient=0.0, SFX=1.0(由脚本接管)
        { 1.0f, 0.0f, 1.0f },
        // Dialogue: BGM=0.6, Ambient=0.4, SFX=1.0
        { 0.6f, 0.4f, 1.0f },
        // Menu: BGM=0.4, Ambient=0.3, SFX=1.0
        { 0.4f, 0.3f, 1.0f },
        // Silence: BGM=0.0, Ambient=1.0, SFX=1.0
        { 0.0f, 1.0f, 1.0f },
    };

    /// <summary>
    /// 查询指定状态和轨道的衰减系数。
    /// </summary>
    public static float GetAttenuation(AudioState state, AudioTrack track)
    {
        return Matrix[(int)state, (int)track];
    }

    /// <summary>
    /// 获取指定状态下所有轨道的衰减系数。
    /// </summary>
    public static (float bgm, float ambient, float sfx) GetAll(AudioState state)
    {
        int s = (int)state;
        return (Matrix[s, 0], Matrix[s, 1], Matrix[s, 2]);
    }
}
