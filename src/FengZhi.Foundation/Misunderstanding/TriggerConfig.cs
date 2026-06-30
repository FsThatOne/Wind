namespace FengZhi.Foundation.Misunderstanding;

/// <summary>单条误会触发配置 — 数据驱动，不硬编码。</summary>
public sealed class TriggerConfig
{
    public required string TriggerId { get; init; }
    public required string TargetNpc { get; init; }
    public required SourceType SourceType { get; init; }
    public required Severity Severity { get; init; }
    public string? MatchEventId { get; init; }
    public string? UnlockFlag { get; init; }
    public int? WindowOverride { get; init; }
}
