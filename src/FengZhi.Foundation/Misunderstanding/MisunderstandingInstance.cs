using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>单条误会实例的完整数据。</summary>
public sealed class MisunderstandingInstance
{
    public required string Id { get; init; }
    public required string TargetNpc { get; init; }
    public required SourceType SourceType { get; init; }
    public Severity Severity { get; internal set; }
    public Transparency Transparency { get; internal set; }
    public int InitialWindow { get; init; }
    public int WindowRemaining { get; internal set; }
    public int EscalationCount { get; internal set; }
    public int CreatedChapter { get; init; }
    public int CreatedDay { get; init; }
    public MisunderstandingState State { get; internal set; } = MisunderstandingState.Dormant;
    public List<string> ResolutionConditions { get; init; } = new();
    public string? UnlockFlag { get; init; }

    /// <summary>severity → misunderstanding_mod 映射。</summary>
    public static int SeverityToMod(Severity severity) => severity switch
    {
        Severity.Minor => -1,
        Severity.Moderate => -2,
        Severity.Severe => -2,
        _ => 0,
    };

    /// <summary>创建一个尚未激活的误会实例，状态转换仍由状态机负责。</summary>
    public static MisunderstandingInstance Create(
        string id,
        string targetNpc,
        SourceType sourceType,
        Severity severity,
        int window,
        int createdChapter,
        int createdDay,
        string? unlockFlag = null)
        => new()
        {
            Id = id,
            TargetNpc = targetNpc,
            SourceType = sourceType,
            Severity = severity,
            InitialWindow = window,
            WindowRemaining = window,
            CreatedChapter = createdChapter,
            CreatedDay = createdDay,
            UnlockFlag = unlockFlag,
        };
}
