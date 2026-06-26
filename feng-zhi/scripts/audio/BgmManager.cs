using Godot;
using FengZhi.Foundation.Audio;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// BGM 管理器。持有双 AudioStreamPlayer 实现等功率 crossfade。
/// 作为 AudioDirector 的子节点运行。
/// </summary>
public partial class BgmManager : Node
{
    private AudioStreamPlayer _playerA = null!;
    private AudioStreamPlayer _playerB = null!;
    private bool _playerAIsActive = true;

    private readonly BgmCrossfadeEngine _engine = new();

    public BgmCrossfadeEngine Engine => _engine;

    public override void _Ready()
    {
        _playerA = CreatePlayer(AudioBusLayout.BgmMain);
        _playerB = CreatePlayer(AudioBusLayout.BgmCrossfade);
    }

    public override void _Process(double delta)
    {
        if (!_engine.IsCrossfading) return;

        float deltaMs = (float)(delta * 1000.0);
        var volumes = _engine.Update(deltaMs);

        var outPlayer = _playerAIsActive ? _playerA : _playerB;
        var inPlayer = _playerAIsActive ? _playerB : _playerA;

        outPlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(volumes.FadeOutVolume);

        if (_engine.PendingTrackId is not ("" or BgmCrossfadeEngine.SilenceTrackId))
            inPlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(volumes.FadeInVolume);

        if (volumes.FadeOutComplete && outPlayer.Playing)
            outPlayer.Stop();

        if (volumes.NewTrackReady)
            _playerAIsActive = !_playerAIsActive;
    }

    /// <summary>
    /// 场景切换时播放新 BGM。
    /// </summary>
    public PlayResult PlayBgm(string trackId, float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        var result = _engine.PlayBgm(trackId, fadeOutMs, fadeInMs);
        if (result == PlayResult.Crossfading || result == PlayResult.FadeToSilence)
            PrepareIncomingPlayer(trackId);
        return result;
    }

    /// <summary>
    /// Override 栈 push（战斗/演出进入）。
    /// </summary>
    public PlayResult PushBgm(string trackId, AudioState state, float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        var result = _engine.PushBgm(trackId, state, fadeOutMs, fadeInMs);
        if (result == PlayResult.Crossfading || result == PlayResult.FadeToSilence)
            PrepareIncomingPlayer(trackId);
        return result;
    }

    /// <summary>
    /// Override 栈 pop（战斗/演出退出）。
    /// </summary>
    public PlayResult PopBgm(float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        var result = _engine.PopBgm(fadeOutMs, fadeInMs);
        if (result == PlayResult.Crossfading || result == PlayResult.FadeToSilence)
        {
            string nextTrack = _engine.PendingTrackId;
            PrepareIncomingPlayer(nextTrack);
        }
        return result;
    }

    private void PrepareIncomingPlayer(string trackId)
    {
        if (trackId is "" or BgmCrossfadeEngine.SilenceTrackId) return;

        var inPlayer = _playerAIsActive ? _playerB : _playerA;
        var stream = GD.Load<AudioStream>($"res://feng-zhi/assets/audio/bgm/{trackId}.ogg");
        if (stream == null)
        {
            GD.PushWarning($"[BgmManager] BGM track not found: {trackId}");
            return;
        }

        inPlayer.Stream = stream;
        inPlayer.VolumeDb = -80f;
        inPlayer.Play();
    }

    private AudioStreamPlayer CreatePlayer(string bus)
    {
        var player = new AudioStreamPlayer();
        player.Bus = bus;
        player.VolumeDb = -80f;
        AddChild(player);
        return player;
    }
}
