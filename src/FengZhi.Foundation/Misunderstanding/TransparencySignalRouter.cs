namespace FengZhi.Foundation.Misunderstanding;

/// <summary>透明度信号路由器 — 将逻辑层信号分发到表现层各 channel。</summary>
public sealed class TransparencySignalRouter : ITransparencySignalEmitter
{
    private readonly IDialogueSignalChannel _dialogue;
    private readonly IPanelSignalChannel _panel;
    private readonly IAmbienceSignalChannel _ambience;

    public TransparencySignalRouter(
        IDialogueSignalChannel dialogue,
        IPanelSignalChannel panel,
        IAmbienceSignalChannel ambience)
    {
        _dialogue = dialogue;
        _panel = panel;
        _ambience = ambience;
    }

    public void EmitHint(string npcId, string instanceId)
    {
        _dialogue.OnHinted(npcId, instanceId);
    }

    public void EmitPerceived(string npcId, string instanceId)
    {
        _panel.OnPerceived(npcId, instanceId);
    }

    public void EmitUrgent(string npcId, string instanceId)
    {
        _panel.OnUrgent(npcId, instanceId);
        _ambience.OnUrgent(npcId, instanceId);
    }
}

/// <summary>对话信号通道接口 — 称呼回退 + 语气着色。</summary>
public interface IDialogueSignalChannel
{
    void OnHinted(string npcId, string instanceId);
}

/// <summary>关系面板信号通道接口 — 云雾标记 + 脉动控制。</summary>
public interface IPanelSignalChannel
{
    void OnPerceived(string npcId, string instanceId);
    void OnUrgent(string npcId, string instanceId);
}

/// <summary>氛围信号通道接口 — 音频 + 色温偏移。</summary>
public interface IAmbienceSignalChannel
{
    void OnUrgent(string npcId, string instanceId);
}
