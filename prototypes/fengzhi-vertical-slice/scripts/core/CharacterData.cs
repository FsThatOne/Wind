// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
namespace FengzhiSlice;

/// <summary>
/// Character combat stats. Minimal for vertical slice.
/// </summary>
public class CharacterData
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = ""; // Literary title (朦胧化)
    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; set; } = 100;
    public int MaxNeili { get; set; } = 5; // 内息 (inner energy)
    public int CurrentNeili { get; set; } = 5;
    public int StaggerCap { get; set; } = 3; // 破绽上限
    public int CurrentStagger { get; set; } = 0;
    public int GongliRank { get; set; } = 1; // 功力等级 1-9
    public string GongliTitle { get; set; } = "初窥门径"; // Literary rank
    
    // Mindset (player only)
    public float Obsession { get; set; } = 0f;  // 执念 (-1 释怀 to +1 执念)
    public float Engagement { get; set; } = 0f; // 入世 (-1 出世 to +1 入世)
    
    public bool IsAlive => CurrentHp > 0;
    public bool CanBurst => CurrentNeili >= 3;
    public bool IsStaggered => CurrentStagger >= StaggerCap;
    public float HpRatio => (float)CurrentHp / MaxHp;
    
    public void TakeDamage(int amount)
    {
        CurrentHp = System.Math.Max(0, CurrentHp - amount);
    }
    
    public void SpendNeili(int amount)
    {
        CurrentNeili = System.Math.Max(0, CurrentNeili - amount);
    }
    
    public void GainNeili(int amount)
    {
        CurrentNeili = System.Math.Min(MaxNeili, CurrentNeili + amount);
    }
    
    public void AddStagger(int amount)
    {
        CurrentStagger = System.Math.Min(StaggerCap, CurrentStagger + amount);
    }
    
    public void DecayStagger()
    {
        // Per prototype finding: -1 stagger per turn if no new stagger added
        if (CurrentStagger > 0) CurrentStagger--;
    }
    
    public void ApplyMindsetShift(float obsessionDelta, float engagementDelta)
    {
        Obsession = System.Math.Clamp(Obsession + obsessionDelta, -1f, 1f);
        Engagement = System.Math.Clamp(Engagement + engagementDelta, -1f, 1f);
    }
    
    public static CharacterData CreatePlayer()
    {
        return new CharacterData
        {
            Name = "无名",
            Title = "风止山庄末代弟子",
            MaxHp = 120,
            CurrentHp = 120,
            MaxNeili = 5,
            CurrentNeili = 5,
            StaggerCap = 4,
            GongliRank = 2,
            GongliTitle = "初窥门径"
        };
    }
    
    public static CharacterData CreateBanditEnemy()
    {
        return new CharacterData
        {
            Name = "山贼头目",
            Title = "落尘谷山贼",
            MaxHp = 80,
            CurrentHp = 80,
            MaxNeili = 3,
            CurrentNeili = 3,
            StaggerCap = 3,
            GongliRank = 1,
            GongliTitle = "粗通武艺"
        };
    }
}
