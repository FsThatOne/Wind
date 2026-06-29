using Godot;
using FengZhi.Foundation.Audio;
using AudioBusLayout = FengZhi.Foundation.Audio.AudioBusLayout;

namespace FengZhi.Scripts.Audio;

/// <summary>
/// SFX 并发池管理器。持有 12 个 AudioStreamPlayer（8 常规 + 4 溢出），
/// 逐帧回收已结束的 slot，通过 SfxPoolEngine 进行优先级仲裁。
/// 作为 AudioDirector 的子节点运行。
/// </summary>
public partial class SfxManager : Node
{
    private AudioStreamPlayer[] _players = null!;
    private readonly SfxPoolEngine _engine = new();

    public SfxPoolEngine Engine => _engine;

    public override void _Ready()
    {
        _players = new AudioStreamPlayer[SfxPoolEngine.MaxPoolSize];
        for (int i = 0; i < SfxPoolEngine.MaxPoolSize; i++)
            _players[i] = CreatePlayer();
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < SfxPoolEngine.MaxPoolSize; i++)
        {
            var slot = _engine.GetSlot(i);
            if (slot.IsActive && !_players[i].Playing)
                _engine.MarkSlotFinished(i);
        }
    }

    /// <summary>
    /// 请求播放 SFX。
    /// </summary>
    public PlaySfxResult PlaySfx(string sfxId, SfxPriority priority, string sourceId, bool bypassCooldown = false)
    {
        float currentTimeMs = (float)(Time.GetTicksMsec());
        var response = _engine.TryPlay(sfxId, priority, sourceId, currentTimeMs, bypassCooldown);

        if (response.Result == PlaySfxResult.Played)
        {
            if (response.EvictedSlotIndex >= 0)
                _players[response.EvictedSlotIndex].Stop();

            StartPlayer(response.SlotIndex, sfxId);
        }

        return response.Result;
    }

    private void StartPlayer(int slotIndex, string sfxId)
    {
        var player = _players[slotIndex];
        var stream = AudioDirector.LoadAudioStream("sfx", sfxId);
        if (stream == null)
        {
            GD.PushWarning($"[SfxManager] SFX not found: {sfxId} (tried .ogg/.mp3)");
            _engine.MarkSlotFinished(slotIndex);
            return;
        }

        player.Stream = stream;
        player.Play();
    }

    /// <summary>
    /// 淡出并停止所有指定 sourceId 的活跃 SFX。
    /// </summary>
    public void FadeOutBySource(string sourceId, float durationMs)
    {
        var slotIndices = _engine.GetActiveSlotsBySource(sourceId);
        float durationSec = durationMs / 1000f;

        foreach (int i in slotIndices)
        {
            var player = _players[i];
            var tween = CreateTween();
            tween.TweenProperty(player, "volume_db", -80f, durationSec);
            int slotIndex = i;
            tween.TweenCallback(Callable.From(() =>
            {
                player.Stop();
                _engine.MarkSlotFinished(slotIndex);
            }));
        }
    }

    private AudioStreamPlayer CreatePlayer()
    {
        var player = new AudioStreamPlayer();
        player.Bus = AudioBusLayout.SfxPool;
        AddChild(player);
        return player;
    }
}
