using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Audio;

/// <summary>
/// 音频用户音量配置。负责四档音量、dB 映射、状态衰减叠加和存档快照。
/// </summary>
public sealed class AudioVolumeSettings : ISaveable
{
    public const string AudioVolumeSaveKey = "audio_volume_settings";
    public const string MasterField = "master";
    public const string BgmField = "bgm";
    public const string AmbientField = "ambient";
    public const string SfxField = "sfx";

    public const float MinDb = -80f;
    public const float MaxDb = 0f;
    public const float DefaultMaster = 1f;
    public const float DefaultBgm = 0.8f;
    public const float DefaultAmbient = 0.7f;
    public const float DefaultSfx = 1f;

    private readonly Dictionary<AudioVolumeTrack, float> _volumes = new()
    {
        [AudioVolumeTrack.Master] = DefaultMaster,
        [AudioVolumeTrack.Bgm] = DefaultBgm,
        [AudioVolumeTrack.Ambient] = DefaultAmbient,
        [AudioVolumeTrack.Sfx] = DefaultSfx
    };

    /// <inheritdoc />
    public string SaveKey => AudioVolumeSaveKey;

    /// <summary>
    /// 设置指定音量轨道，输入范围会 clamp 到 0.0-1.0。
    /// </summary>
    public void SetVolume(AudioVolumeTrack track, float value01)
    {
        _volumes[track] = Math.Clamp(value01, 0f, 1f);
    }

    /// <summary>
    /// 读取指定音量轨道的用户配置值。
    /// </summary>
    public float GetVolume(AudioVolumeTrack track)
    {
        return _volumes[track];
    }

    /// <summary>
    /// 将设置 UI 的 0-100 数值写入音量轨道。
    /// </summary>
    public void SetVolumePercent(AudioVolumeTrack track, float value0To100)
    {
        SetVolume(track, value0To100 / 100f);
    }

    /// <summary>
    /// 计算指定状态下写入 AudioServer bus 的有效线性音量。
    /// Master 不参与 state_attenuation；其他轨道按 GDD F2 公式相乘。
    /// </summary>
    public float GetEffectiveVolume(AudioVolumeTrack track, AudioState state)
    {
        float userVolume = GetVolume(track);
        if (track == AudioVolumeTrack.Master)
            return userVolume;

        return userVolume * AudioStateAttenuation.GetAttenuation(state, ToAudioTrack(track));
    }

    /// <summary>
    /// 将线性音量映射到 AudioServer dB。0 或更低值映射为 -80dB 静音。
    /// </summary>
    public static float LinearToDb(float linearVolume)
    {
        if (linearVolume <= 0f)
            return MinDb;

        float clamped = Math.Clamp(linearVolume, 0f, 1f);
        return 20f * MathF.Log10(clamped);
    }

    /// <inheritdoc />
    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        snapshot.Values[MasterField] = JsonSerializer.SerializeToElement(GetVolume(AudioVolumeTrack.Master));
        snapshot.Values[BgmField] = JsonSerializer.SerializeToElement(GetVolume(AudioVolumeTrack.Bgm));
        snapshot.Values[AmbientField] = JsonSerializer.SerializeToElement(GetVolume(AudioVolumeTrack.Ambient));
        snapshot.Values[SfxField] = JsonSerializer.SerializeToElement(GetVolume(AudioVolumeTrack.Sfx));
        return snapshot;
    }

    /// <inheritdoc />
    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;

        SetVolume(AudioVolumeTrack.Master, ReadFloatOrDefault(snapshot, MasterField, DefaultMaster));
        SetVolume(AudioVolumeTrack.Bgm, ReadFloatOrDefault(snapshot, BgmField, DefaultBgm));
        SetVolume(AudioVolumeTrack.Ambient, ReadFloatOrDefault(snapshot, AmbientField, DefaultAmbient));
        SetVolume(AudioVolumeTrack.Sfx, ReadFloatOrDefault(snapshot, SfxField, DefaultSfx));
    }

    private static float ReadFloatOrDefault(SaveSnapshot snapshot, string field, float fallback)
    {
        if (!snapshot.Values.TryGetValue(field, out var element))
            return fallback;

        try
        {
            return element.ValueKind == JsonValueKind.Number
                ? element.GetSingle()
                : fallback;
        }
        catch (FormatException)
        {
            return fallback;
        }
        catch (InvalidOperationException)
        {
            return fallback;
        }
    }

    private static AudioTrack ToAudioTrack(AudioVolumeTrack track)
    {
        return track switch
        {
            AudioVolumeTrack.Bgm => AudioTrack.Bgm,
            AudioVolumeTrack.Ambient => AudioTrack.Ambient,
            AudioVolumeTrack.Sfx => AudioTrack.Sfx,
            _ => throw new ArgumentOutOfRangeException(nameof(track), track, "Master does not map to state attenuation.")
        };
    }
}

/// <summary>
/// 音频总线音量写入接口。Godot 层写入 AudioServer，测试层可替换为记录器。
/// </summary>
public interface IAudioBusVolumeWriter
{
    /// <summary>
    /// 将指定 bus 写为 dB 音量。
    /// </summary>
    void SetBusVolumeDb(string busName, float volumeDb);
}

/// <summary>
/// 音频音量应用器。把用户音量和状态衰减计算结果写入总线。
/// </summary>
public sealed class AudioVolumeController : ISaveable
{
    private readonly IAudioBusVolumeWriter _writer;
    private readonly AudioVolumeSettings _settings = new();
    private AudioState _currentState = AudioState.Exploration;

    public AudioVolumeController(IAudioBusVolumeWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <inheritdoc />
    public string SaveKey => _settings.SaveKey;

    /// <summary>
    /// 当前用户音量配置。
    /// </summary>
    public AudioVolumeSettings Settings => _settings;

    /// <summary>
    /// 当前音频状态，用于计算 state_attenuation。
    /// </summary>
    public AudioState CurrentState => _currentState;

    /// <summary>
    /// 设置用户音量（0.0-1.0）并立即刷新所有相关总线。
    /// </summary>
    public void SetVolume(AudioVolumeTrack track, float value01)
    {
        _settings.SetVolume(track, value01);
        Apply();
    }

    /// <summary>
    /// 读取用户音量配置。
    /// </summary>
    public float GetVolume(AudioVolumeTrack track)
    {
        return _settings.GetVolume(track);
    }

    /// <summary>
    /// 应用新的音频状态并刷新有效音量。
    /// </summary>
    public void ApplyState(AudioState state)
    {
        _currentState = state;
        Apply();
    }

    /// <summary>
    /// 按当前状态刷新 Master/BGM/Ambient/SFX 四档 bus。
    /// </summary>
    public void Apply()
    {
        WriteBus(AudioBusLayout.Master, AudioVolumeTrack.Master);
        WriteBus(AudioBusLayout.Bgm, AudioVolumeTrack.Bgm);
        WriteBus(AudioBusLayout.Ambient, AudioVolumeTrack.Ambient);
        WriteBus(AudioBusLayout.Sfx, AudioVolumeTrack.Sfx);
    }

    /// <inheritdoc />
    public SaveSnapshot Serialize()
    {
        return _settings.Serialize();
    }

    /// <inheritdoc />
    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _settings.Deserialize(snapshot, version);
        Apply();
    }

    private void WriteBus(string busName, AudioVolumeTrack track)
    {
        float effective = _settings.GetEffectiveVolume(track, _currentState);
        _writer.SetBusVolumeDb(busName, AudioVolumeSettings.LinearToDb(effective));
    }
}

/// <summary>
/// 设置界面暴露的四档音频音量。
/// </summary>
public enum AudioVolumeTrack
{
    Master,
    Bgm,
    Ambient,
    Sfx
}
