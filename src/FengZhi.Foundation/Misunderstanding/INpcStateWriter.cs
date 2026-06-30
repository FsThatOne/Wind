namespace FengZhi.Foundation.Misunderstanding;

/// <summary>NPC 状态写入接口 — 误会系统唯一的外部写出通道。</summary>
public interface INpcStateWriter
{
    void SetMisunderstandingMod(string npcId, int value);
    void ApplyTemporaryBonus(string npcId, int bonusValue, int durationDays);
}
