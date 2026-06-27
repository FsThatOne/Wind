using Godot;
using FengZhi.Foundation.Audio;
using AudioBusLayout = FengZhi.Foundation.Audio.AudioBusLayout;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// BGM 管理器。持有双 AudioStreamPlayer 实现等功率 crossfade。
/// 作为 AudioDirector 的子节点运行。
/// </summary>
public partial class BgmManager : Node
{
    private AudioStreamPlayer _playerA = null!;
    private AudioStreamPlayer _playerB = null!;
    private AudioStreamPlayer _motifPlayer = null!;
    private bool _playerAIsActive = true;
    private string? _loadedMotifTrackId;

    private readonly BgmCrossfadeEngine _engine = new();
    private readonly MotifOverlayEngine _motifEngine = new();

    public BgmCrossfadeEngine Engine => _engine;
    public MotifOverlayEngine MotifEngine => _motifEngine;

    public AudioStreamPlayer ActivePlayer => _playerAIsActive ? _playerA : _playerB;
    public AudioStreamPlayer IncomingPlayer => _playerAIsActive ? _playerB : _playerA;
    public AudioStreamPlayer MotifPlayer => _motifPlayer;

    public override void _Ready()
    {
        _playerA = CreatePlayer(AudioBusLayout.BgmMain);
        _playerB = CreatePlayer(AudioBusLayout.BgmCrossfade);
        _motifPlayer = CreatePlayer(AudioBusLayout.Bgm);
    }

    public override void _Process(double delta)
    {
        float deltaMs = (float)(delta * 1000.0);
        MotifOverlayFrame? motifFrame = null;

        if (_motifEngine.IsMotifActive)
        {
            motifFrame = _motifEngine.Update(deltaMs);
            ApplyMotifFrame(motifFrame.Value);
        }

        float motifBgmMultiplier = _motifEngine.IsMotifActive ? _motifEngine.BgmVolume : 1f;

        if (!_engine.IsCrossfading)
        {
            if (motifFrame.HasValue)
                ActivePlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(motifBgmMultiplier);
            return;
        }

        var volumes = _engine.Update(deltaMs);

        var outPlayer = _playerAIsActive ? _playerA : _playerB;
        var inPlayer = _playerAIsActive ? _playerB : _playerA;

        outPlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(volumes.FadeOutVolume * motifBgmMultiplier);

        if (_engine.PendingTrackId is not ("" or BgmCrossfadeEngine.SilenceTrackId))
            inPlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(volumes.FadeInVolume * motifBgmMultiplier);

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
        bool wasCrossfading = _engine.IsCrossfading;
        var result = _engine.PlayBgm(trackId, fadeOutMs, fadeInMs);
        if (result is PlayResult.Crossfading or PlayResult.FadeToSilence)
        {
            if (wasCrossfading) StopAndSwap();
            PrepareIncomingPlayer(trackId);
        }
        return result;
    }

    /// <summary>
    /// Override 栈 push（战斗/演出进入）。
    /// </summary>
    public PlayResult PushBgm(string trackId, AudioState state, float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        bool wasCrossfading = _engine.IsCrossfading;
        var result = _engine.PushBgm(trackId, state, fadeOutMs, fadeInMs);
        if (result is PlayResult.Crossfading or PlayResult.FadeToSilence)
        {
            if (wasCrossfading) StopAndSwap();
            PrepareIncomingPlayer(trackId);
        }
        return result;
    }

    /// <summary>
    /// Override 栈 pop（战斗/演出退出）。
    /// </summary>
    public PlayResult PopBgm(float fadeOutMs = 1500f, float fadeInMs = 800f)
    {
        bool wasCrossfading = _engine.IsCrossfading;
        var result = _engine.PopBgm(fadeOutMs, fadeInMs);
        if (result is PlayResult.Crossfading or PlayResult.FadeToSilence)
        {
            if (wasCrossfading) StopAndSwap();
            PrepareIncomingPlayer(_engine.PendingTrackId);
        }
        return result;
    }

    /// <summary>
    /// 战斗内段落切换用。只执行 crossfade，不修改 Override 栈。
    /// </summary>
    public void CrossfadeBgm(string trackId, float fadeOutMs, float fadeInMs)
    {
        if (string.IsNullOrEmpty(trackId)) return;
        bool wasCrossfading = _engine.IsCrossfading;
        _engine.StartCrossfadeOnly(trackId, fadeOutMs, fadeInMs);
        if (wasCrossfading) StopAndSwap();
        PrepareIncomingPlayer(trackId);
    }

    /// <summary>
    /// 播放女主 motif overlay。Motif 不进入 BGM Override 栈。
    /// </summary>
    public MotifPlayResult PlayMotif(string characterId, float fadeMs = MotifOverlayEngine.DefaultFadeMs)
    {
        var result = _motifEngine.PlayMotif(characterId, fadeMs);
        if (result == MotifPlayResult.FadeInStarted)
            PrepareMotifPlayer(_motifEngine.ActiveTrackId);
        return result;
    }

    /// <summary>
    /// 停止女主 motif overlay，并恢复场景 BGM 音量。
    /// </summary>
    public MotifPlayResult StopMotif(float fadeMs = MotifOverlayEngine.DefaultFadeMs)
    {
        return _motifEngine.StopMotif(fadeMs);
    }

    private void StopAndSwap()
    {
        var outPlayer = _playerAIsActive ? _playerA : _playerB;
        outPlayer.Stop();
        _playerAIsActive = !_playerAIsActive;
    }

    private void PrepareIncomingPlayer(string trackId)
    {
        if (trackId is "" or BgmCrossfadeEngine.SilenceTrackId) return;

        var inPlayer = _playerAIsActive ? _playerB : _playerA;
        var stream = GD.Load<AudioStream>($"res://assets/audio/bgm/{trackId}.ogg");
        if (stream == null)
        {
            GD.PushWarning($"[BgmManager] BGM track not found: {trackId}");
            return;
        }

        inPlayer.Stream = stream;
        inPlayer.VolumeDb = -80f;
        inPlayer.Play();
    }

    private void ApplyMotifFrame(MotifOverlayFrame frame)
    {
        if (frame.Phase == MotifOverlayPhase.Idle)
        {
            if (_motifPlayer.Playing)
                _motifPlayer.Stop();
            _loadedMotifTrackId = null;
            return;
        }

        PrepareMotifPlayer(frame.ActiveTrackId);
        _motifPlayer.VolumeDb = BgmCrossfadeEngine.LinearToDb(frame.MotifVolume);
    }

    private void PrepareMotifPlayer(string? trackId)
    {
        if (string.IsNullOrEmpty(trackId) || _loadedMotifTrackId == trackId)
            return;

        var stream = GD.Load<AudioStream>($"res://assets/audio/bgm/{trackId}.ogg");
        if (stream == null)
        {
            GD.PushWarning($"[BgmManager] Motif track not found: {trackId}");
            _loadedMotifTrackId = null;
            if (_motifPlayer.Playing)
                _motifPlayer.Stop();
            _motifEngine.Reset();
            return;
        }

        _loadedMotifTrackId = trackId;
        _motifPlayer.Stream = stream;
        _motifPlayer.VolumeDb = -80f;
        _motifPlayer.Play();
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
