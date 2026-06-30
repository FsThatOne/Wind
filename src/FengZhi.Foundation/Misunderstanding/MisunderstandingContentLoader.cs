using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会配置加载器 — 从 JSON 文件加载触发配置和澄清条件。</summary>
public static class MisunderstandingContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static List<TriggerConfigData> LoadTriggers(string jsonContent)
    {
        return JsonSerializer.Deserialize<List<TriggerConfigData>>(jsonContent, JsonOptions)
               ?? new List<TriggerConfigData>();
    }

    public static List<ResolutionConfigData> LoadResolutions(string jsonContent)
    {
        return JsonSerializer.Deserialize<List<ResolutionConfigData>>(jsonContent, JsonOptions)
               ?? new List<ResolutionConfigData>();
    }

    public static AddressTableData LoadAddressTable(string jsonContent)
    {
        return JsonSerializer.Deserialize<AddressTableData>(jsonContent, JsonOptions)
               ?? throw new InvalidOperationException("称呼表 JSON 无效");
    }

    public static LiterarySignalData LoadLiterarySignals(string jsonContent)
    {
        return JsonSerializer.Deserialize<LiterarySignalData>(jsonContent, JsonOptions)
               ?? throw new InvalidOperationException("文学信号 JSON 无效");
    }

    /// <summary>将加载的 trigger data 转为运行时 TriggerConfig。</summary>
    public static TriggerConfig ToTriggerConfig(TriggerConfigData data)
    {
        return new TriggerConfig
        {
            TriggerId = data.TriggerId,
            TargetNpc = data.TargetNpc,
            SourceType = data.SourceType,
            Severity = data.Severity,
            MatchEventId = data.MatchEventId,
            WindowOverride = data.WindowOverride,
        };
    }
}

public sealed class TriggerConfigData
{
    [JsonPropertyName("trigger_id")]
    public string TriggerId { get; set; } = "";

    [JsonPropertyName("target_npc")]
    public string TargetNpc { get; set; } = "";

    [JsonPropertyName("source_type")]
    public SourceType SourceType { get; set; }

    [JsonPropertyName("severity")]
    public Severity Severity { get; set; }

    [JsonPropertyName("match_event_id")]
    public string? MatchEventId { get; set; }

    [JsonPropertyName("window_override")]
    public int? WindowOverride { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ResolutionConfigData
{
    [JsonPropertyName("instance_pattern")]
    public string InstancePattern { get; set; } = "";

    [JsonPropertyName("resolution_conditions")]
    public List<string> ResolutionConditions { get; set; } = new();

    [JsonPropertyName("unlock_flag")]
    public string? UnlockFlag { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class AddressTableData
{
    [JsonPropertyName("npc_id")]
    public string NpcId { get; set; } = "";

    [JsonPropertyName("intimate")]
    public string Intimate { get; set; } = "";

    [JsonPropertyName("distant")]
    public string Distant { get; set; } = "";

    [JsonPropertyName("broken")]
    public string Broken { get; set; } = "";
}

public sealed class LiterarySignalData
{
    [JsonPropertyName("npc_id")]
    public string NpcId { get; set; } = "";

    [JsonPropertyName("signals")]
    public SignalTexts Signals { get; set; } = new();
}

public sealed class SignalTexts
{
    [JsonPropertyName("hinted")]
    public string Hinted { get; set; } = "";

    [JsonPropertyName("perceived")]
    public string Perceived { get; set; } = "";

    [JsonPropertyName("urgent")]
    public string Urgent { get; set; } = "";
}
