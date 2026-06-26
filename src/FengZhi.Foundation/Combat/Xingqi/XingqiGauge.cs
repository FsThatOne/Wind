namespace FengZhi.Foundation.Combat.Xingqi;

/// <summary>
/// 单角色行气条模型。
/// </summary>
public sealed class XingqiGauge
{
    public string CombatantId { get; }
    public int CurrentValue { get; private set; }

    public XingqiGauge(string combatantId, int initialValue = 0)
    {
        CombatantId = combatantId;
        CurrentValue = initialValue;
    }

    public bool IsReady(int threshold) => CurrentValue >= threshold;

    public void Advance(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        CurrentValue += amount;
    }

    /// <summary>
    /// 行动后行气回落。
    /// </summary>
    /// <param name="retainedPercent">保留百分比（0~retainedCap）</param>
    /// <param name="threshold">行气阈值</param>
    /// <param name="retainedCap">保留上限百分比</param>
    public void Reset(float retainedPercent = 0f, int threshold = 100, float retainedCap = 0.30f)
    {
        if (retainedPercent <= 0f)
        {
            CurrentValue = 0;
            return;
        }

        float effectivePercent = Math.Min(retainedPercent, retainedCap);
        int retained = (int)(threshold * effectivePercent);
        CurrentValue = Math.Min(retained, (int)(threshold * retainedCap));
    }
}
