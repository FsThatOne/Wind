namespace FengZhi.Foundation.TimeSystem;

/// <summary>体力状态</summary>
public enum StaminaState
{
    /// <summary>体力 > 50% max</summary>
    Vigorous,
    /// <summary>体力 ≤ 50% 且 > 0</summary>
    Fatigued,
    /// <summary>体力 = 0</summary>
    Exhausted
}

/// <summary>
/// 体力池管理。纯逻辑 POCO，不依赖 Node。
/// GDD: F1 上限计算, F5 状态判定, F3 休息恢复。
/// </summary>
public sealed class StaminaSystem
{
    public const int DefaultBaseStamina = 100;
    public const int DefaultStaminaPerCon = 5;
    public const float ExhaustedThreshold = 0f;
    public const float FatiguedThreshold = 0.5f;
    public const float DefaultRestRatio = 0.3f; // 小憩 30%

    private int _baseStamina;
    private int _staminaPerCon;

    /// <summary>当前体力</summary>
    public float CurrentStamina { get; private set; }

    /// <summary>当前体魄值（外部传入）</summary>
    public int Constitution { get; private set; }

    /// <summary>F1: max_stamina = base + constitution * stamina_per_con</summary>
    public float MaxStamina => _baseStamina + Constitution * _staminaPerCon;

    /// <summary>当前体力百分比 [0, 1]</summary>
    public float StaminaPercent => MaxStamina > 0 ? CurrentStamina / MaxStamina : 0f;

    /// <summary>F5: 体力状态判定</summary>
    public StaminaState CurrentState
    {
        get
        {
            if (CurrentStamina <= ExhaustedThreshold) return StaminaState.Exhausted;
            if (StaminaPercent <= FatiguedThreshold) return StaminaState.Fatigued;
            return StaminaState.Vigorous;
        }
    }

    public StaminaSystem(int constitution = 10, int baseStamina = DefaultBaseStamina, int staminaPerCon = DefaultStaminaPerCon)
    {
        _baseStamina = baseStamina;
        _staminaPerCon = staminaPerCon;
        Constitution = constitution;
        CurrentStamina = MaxStamina;
    }

    /// <summary>
    /// 消耗体力。力竭状态下不再扣除。
    /// 返回实际消耗量。
    /// </summary>
    public float ConsumeStamina(float amount)
    {
        if (amount <= 0) return 0;
        if (CurrentStamina <= 0) return 0; // 力竭不再扣除

        float actual = Math.Min(amount, CurrentStamina);
        CurrentStamina -= actual;
        if (CurrentStamina < 0) CurrentStamina = 0;
        return actual;
    }

    /// <summary>
    /// 恢复体力，不超过上限。
    /// 返回实际恢复量。
    /// </summary>
    public float RestoreStamina(float amount)
    {
        if (amount <= 0) return 0;

        float gap = MaxStamina - CurrentStamina;
        float actual = Math.Min(amount, gap);
        CurrentStamina += actual;
        return actual;
    }

    /// <summary>
    /// F3: 按 rest_ratio 恢复，不超过缺口。
    /// 用于小憩等场景。
    /// </summary>
    public float RestByRatio(float restRatio)
    {
        float amount = restRatio * MaxStamina;
        return RestoreStamina(amount);
    }

    /// <summary>
    /// 完全恢复至上限（过夜）。
    /// </summary>
    public float RestFull()
    {
        float gap = MaxStamina - CurrentStamina;
        CurrentStamina = MaxStamina;
        return gap;
    }

    /// <summary>
    /// 体魄变化时重算上限。当前体力不主动削减（AC6）。
    /// </summary>
    public void UpdateConstitution(int newConstitution)
    {
        Constitution = newConstitution;
        // 当前体力不削减，但也不能超过新上限
        // AC6 说"不主动削减"，所以只 clamp 上限
        if (CurrentStamina > MaxStamina)
        {
            CurrentStamina = MaxStamina;
        }
    }

    /// <summary>设置状态（存档恢复用）</summary>
    public void SetState(float currentStamina, int constitution)
    {
        Constitution = constitution;
        CurrentStamina = Math.Clamp(currentStamina, 0, MaxStamina);
    }
}
