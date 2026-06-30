namespace FengZhi.Foundation.Misunderstanding;

/// <summary>对话效果写入接口 — 地板保护下仍可影响对话措辞和选项锁定。</summary>
public interface IDialogueEffectWriter
{
    void SetMisunderstandingEffect(string npcId, int effectLevel);
}
