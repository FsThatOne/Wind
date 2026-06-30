using System;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>地板保护服务 — 施加 mod 前钳位，确保态度不低于里程碑地板。</summary>
public sealed class FloorProtectionService
{
    private readonly IRomanceService _romance;
    private readonly IDialogueEffectWriter _dialogueEffect;

    public FloorProtectionService(IRomanceService romance, IDialogueEffectWriter dialogueEffect)
    {
        _romance = romance;
        _dialogueEffect = dialogueEffect;
    }

    /// <summary>计算钳位后的 effective_mod。地板保护下仍写出对话效果。</summary>
    public int ComputeEffectiveMod(string npcId, int rawMod)
    {
        int floor = _romance.GetMilestoneFloor(npcId);
        int currentScore = _romance.GetAttitudeScore(npcId);

        int effectiveMod = Math.Max(rawMod, floor - currentScore);

        if (effectiveMod > rawMod)
        {
            int effectLevel = Math.Abs(rawMod);
            _dialogueEffect.SetMisunderstandingEffect(npcId, effectLevel);
        }

        return effectiveMod;
    }
}
