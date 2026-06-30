namespace FengZhi.Foundation.Misunderstanding;

/// <summary>透明度信号发射器接口 — 由 Presentation 层实现。</summary>
public interface ITransparencySignalEmitter
{
    void EmitHint(string npcId, string instanceId);
    void EmitPerceived(string npcId, string instanceId);
    void EmitUrgent(string npcId, string instanceId);
}
