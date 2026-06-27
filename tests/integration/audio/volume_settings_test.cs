using System.Text.Json;
using Xunit;
using FengZhi.Foundation.Audio;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.SaveSystem;
using FengZhi.Foundation.StateMachine;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 008: 设置音量响应 + 持久化集成测试。
/// 覆盖用户音量、状态衰减、存档快照和 Master 静音下 FSM 运转。
/// </summary>
public class VolumeSettingsTest
{
    private readonly AudioVolumeSettings _settings = new();

    // === AC10: BGM 滑块实时音量映射 ===

    [Fact]
    public void AC10_BgmSliderFrom80To30_MapsToExpectedDb()
    {
        _settings.SetVolumePercent(AudioVolumeTrack.Bgm, 80f);
        Assert.Equal(-1.94f, AudioVolumeSettings.LinearToDb(_settings.GetVolume(AudioVolumeTrack.Bgm)), 2);

        _settings.SetVolumePercent(AudioVolumeTrack.Bgm, 30f);

        Assert.Equal(0.3f, _settings.GetVolume(AudioVolumeTrack.Bgm), 2);
        Assert.Equal(-10.46f, AudioVolumeSettings.LinearToDb(_settings.GetVolume(AudioVolumeTrack.Bgm)), 2);
    }

    [Fact]
    public void AC10_SetVolume_WritesBgmBusImmediately()
    {
        var writer = new RecordingBusWriter();
        var controller = new AudioVolumeController(writer);

        controller.SetVolume(AudioVolumeTrack.Bgm, 0.3f);

        Assert.Equal(-10.46f, writer.LastDb(AudioBusLayout.Bgm), 2);
    }

    // === 4 档滑块独立调节 ===

    [Fact]
    public void FourVolumeTracks_AdjustIndependently()
    {
        _settings.SetVolume(AudioVolumeTrack.Master, 0.1f);
        _settings.SetVolume(AudioVolumeTrack.Bgm, 0.3f);
        _settings.SetVolume(AudioVolumeTrack.Ambient, 0.5f);
        _settings.SetVolume(AudioVolumeTrack.Sfx, 0.9f);

        Assert.Equal(0.1f, _settings.GetVolume(AudioVolumeTrack.Master));
        Assert.Equal(0.3f, _settings.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(0.5f, _settings.GetVolume(AudioVolumeTrack.Ambient));
        Assert.Equal(0.9f, _settings.GetVolume(AudioVolumeTrack.Sfx));
    }

    [Fact]
    public void Defaults_MatchStoryFallbackValues()
    {
        Assert.Equal(AudioVolumeSettings.DefaultMaster, _settings.GetVolume(AudioVolumeTrack.Master));
        Assert.Equal(AudioVolumeSettings.DefaultBgm, _settings.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(AudioVolumeSettings.DefaultAmbient, _settings.GetVolume(AudioVolumeTrack.Ambient));
        Assert.Equal(AudioVolumeSettings.DefaultSfx, _settings.GetVolume(AudioVolumeTrack.Sfx));
    }

    // === 音量值 0-100 映射到 -80dB ~ 0dB ===

    [Theory]
    [InlineData(0f, -80f)]
    [InlineData(1f, 0f)]
    [InlineData(0.5f, -6.02f)]
    [InlineData(0.3f, -10.46f)]
    public void LinearToDb_MapsExpectedRange(float linear, float expectedDb)
    {
        Assert.Equal(expectedDb, AudioVolumeSettings.LinearToDb(linear), 2);
    }

    [Fact]
    public void SetVolume_ClampsValuesToZeroOne()
    {
        _settings.SetVolume(AudioVolumeTrack.Bgm, -1f);
        _settings.SetVolume(AudioVolumeTrack.Sfx, 2f);

        Assert.Equal(0f, _settings.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(1f, _settings.GetVolume(AudioVolumeTrack.Sfx));
    }

    // === state_attenuation 叠加 ===

    [Fact]
    public void DialogueState_EffectiveVolume_MultipliesUserVolumeAndAttenuation()
    {
        _settings.SetVolume(AudioVolumeTrack.Bgm, 0.8f);

        float effective = _settings.GetEffectiveVolume(AudioVolumeTrack.Bgm, AudioState.Dialogue);

        Assert.Equal(0.48f, effective, 2);
        Assert.Equal(-6.38f, AudioVolumeSettings.LinearToDb(effective), 2);
    }

    [Fact]
    public void DialogueState_SetVolume_WritesEffectiveBgmBusDb()
    {
        var writer = new RecordingBusWriter();
        var controller = new AudioVolumeController(writer);
        controller.ApplyState(AudioState.Dialogue);

        controller.SetVolume(AudioVolumeTrack.Bgm, 0.8f);

        Assert.Equal(-6.38f, writer.LastDb(AudioBusLayout.Bgm), 2);
    }

    [Fact]
    public void MasterVolume_DoesNotApplyStateAttenuation()
    {
        _settings.SetVolume(AudioVolumeTrack.Master, 0.5f);

        Assert.Equal(0.5f, _settings.GetEffectiveVolume(AudioVolumeTrack.Master, AudioState.Menu));
    }

    // === 持久化到用户存档，重启后恢复 ===

    [Fact]
    public void SaveSnapshot_RestoresFourVolumeTracks()
    {
        _settings.SetVolume(AudioVolumeTrack.Master, 0.9f);
        _settings.SetVolume(AudioVolumeTrack.Bgm, 0.5f);
        _settings.SetVolume(AudioVolumeTrack.Ambient, 0.4f);
        _settings.SetVolume(AudioVolumeTrack.Sfx, 0.8f);

        SaveSnapshot snapshot = _settings.Serialize();
        var restored = new AudioVolumeSettings();
        restored.Deserialize(snapshot, version: 1);

        Assert.Equal(0.9f, restored.GetVolume(AudioVolumeTrack.Master));
        Assert.Equal(0.5f, restored.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(0.4f, restored.GetVolume(AudioVolumeTrack.Ambient));
        Assert.Equal(0.8f, restored.GetVolume(AudioVolumeTrack.Sfx));
    }

    [Fact]
    public async Task SaveManagerRoundtrip_RestoresAudioVolumeController()
    {
        var store = new InMemorySavePayloadStore();
        var source = new AudioVolumeController(new RecordingBusWriter());
        source.SetVolume(AudioVolumeTrack.Bgm, 0.5f);
        source.SetVolume(AudioVolumeTrack.Sfx, 0.8f);

        var saveManager = new SaveManager(store, new EventBus());
        saveManager.RegisterSerializer(source);
        var saveResult = await saveManager.SaveGameAsync(SaveSlotId.Manual(1));

        var restoredWriter = new RecordingBusWriter();
        var restored = new AudioVolumeController(restoredWriter);
        var loadManager = new SaveManager(store, new EventBus());
        loadManager.RegisterSerializer(restored);
        var loadResult = await loadManager.LoadGameAsync(SaveSlotId.Manual(1));

        Assert.True(saveResult.Success);
        Assert.True(loadResult.Success);
        Assert.Equal(0.5f, restored.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(0.8f, restored.GetVolume(AudioVolumeTrack.Sfx));
        Assert.Equal(-6.02f, restoredWriter.LastDb(AudioBusLayout.Bgm), 2);
        Assert.Equal(-1.94f, restoredWriter.LastDb(AudioBusLayout.Sfx), 2);
    }

    [Fact]
    public void SaveSnapshot_MissingOrMalformedFields_FallBackToDefaults()
    {
        var snapshot = new SaveSnapshot();
        snapshot.Values[AudioVolumeSettings.BgmField] = JsonSerializer.SerializeToElement("bad");
        snapshot.Values[AudioVolumeSettings.SfxField] = JsonSerializer.SerializeToElement(0.25f);

        _settings.SetVolume(AudioVolumeTrack.Master, 0.2f);
        _settings.SetVolume(AudioVolumeTrack.Bgm, 0.2f);
        _settings.SetVolume(AudioVolumeTrack.Ambient, 0.2f);
        _settings.SetVolume(AudioVolumeTrack.Sfx, 0.2f);
        _settings.Deserialize(snapshot, version: 1);

        Assert.Equal(AudioVolumeSettings.DefaultMaster, _settings.GetVolume(AudioVolumeTrack.Master));
        Assert.Equal(AudioVolumeSettings.DefaultBgm, _settings.GetVolume(AudioVolumeTrack.Bgm));
        Assert.Equal(AudioVolumeSettings.DefaultAmbient, _settings.GetVolume(AudioVolumeTrack.Ambient));
        Assert.Equal(0.25f, _settings.GetVolume(AudioVolumeTrack.Sfx));
    }

    [Fact]
    public void AudioVolumeSettings_ImplementsSaveableContract()
    {
        ISaveable saveable = _settings;

        Assert.Equal(AudioVolumeSettings.AudioVolumeSaveKey, saveable.SaveKey);
    }

    // === Master=0 时状态机正常运转 ===

    [Fact]
    public void MasterZero_FsmStillTransitionsNormally()
    {
        _settings.SetVolume(AudioVolumeTrack.Master, 0f);
        var fsm = AudioStateMachineFactory.Create();

        Assert.True(fsm.TryTransition(AudioTriggers.EnterCombat));
        Assert.True(fsm.TryTransition(AudioTriggers.ExitCombat));
        Assert.True(fsm.TryTransition(AudioTriggers.StartDialogue));

        Assert.Equal(AudioState.Dialogue, fsm.CurrentState);
        Assert.Equal(AudioVolumeSettings.MinDb, AudioVolumeSettings.LinearToDb(_settings.GetEffectiveVolume(AudioVolumeTrack.Master, fsm.CurrentState)));
    }

    [Fact]
    public void MasterZero_WritesMasterBusMuteOnly()
    {
        var writer = new RecordingBusWriter();
        var controller = new AudioVolumeController(writer);

        controller.SetVolume(AudioVolumeTrack.Master, 0f);

        Assert.Equal(AudioVolumeSettings.MinDb, writer.LastDb(AudioBusLayout.Master));
        Assert.Equal(-1.94f, writer.LastDb(AudioBusLayout.Bgm), 2);
    }

    private sealed class RecordingBusWriter : IAudioBusVolumeWriter
    {
        private readonly Dictionary<string, float> _lastDbByBus = new(StringComparer.Ordinal);

        public void SetBusVolumeDb(string busName, float volumeDb)
        {
            _lastDbByBus[busName] = volumeDb;
        }

        public float LastDb(string busName)
        {
            return _lastDbByBus[busName];
        }
    }
}
