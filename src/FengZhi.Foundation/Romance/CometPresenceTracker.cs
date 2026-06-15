using FengZhi.Foundation.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Four comet-presence counters persisted by NPC State and interpreted by Romance.
/// </summary>
public sealed record CometPresenceCounters(
    int SignsDiscovered = 0,
    int LettersReceived = 0,
    int RumorsHeard = 0,
    int EncountersHad = 0
);

/// <summary>
/// Identifies which comet-presence channel should be incremented.
/// </summary>
public enum CometPresenceKind
{
    Sign,
    Letter,
    Rumor,
    Encounter
}

/// <summary>
/// Tuning values for comet rumor chance calculation.
/// </summary>
public sealed class CometPresenceTuning
{
    [YamlMember(Alias = "rumor_base_chance")]
    public float RumorBaseChance { get; set; } = 0.3f;

    [YamlMember(Alias = "rumor_same_region_bonus")]
    public float RumorSameRegionBonus { get; set; } = 0.4f;

    [YamlMember(Alias = "rumor_absence_bonus")]
    public float RumorAbsenceBonus { get; set; } = 0.2f;

    [YamlMember(Alias = "rumor_absence_threshold_days")]
    public int RumorAbsenceThresholdDays { get; set; } = 7;

    [YamlMember(Alias = "rumor_cap")]
    public float RumorCap { get; set; } = 0.8f;
}

/// <summary>
/// Loads comet-presence tuning from the ADR-0003 YAML data pipeline.
/// </summary>
public sealed class CometPresenceTuningLoader
{
    public const string DefaultPath = "assets/data/romance/comet-presence-tuning.yaml";

    private readonly IDeserializer _deserializer;

    public CometPresenceTuningLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>Loads and validates comet-presence tuning from a YAML document.</summary>
    public CometPresenceTuning Load(string yaml, string filePath)
    {
        try
        {
            var tuning = _deserializer.Deserialize<CometPresenceTuning>(yaml);
            if (tuning == null)
                throw new DataLoadException(filePath, null, "YAML deserialized to null.");
            Validate(tuning, filePath);
            return tuning;
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, ex.Message, ex);
        }
    }

    /// <summary>Loads comet-presence tuning and registers it as a runtime read-only DataRegistry table.</summary>
    public void LoadAll(string yaml, DataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var tuning = Load(yaml, DefaultPath);
        registry.RegisterTable<CometPresenceTuning>(new CometPresenceTuningTable(tuning));
    }

    private static void Validate(CometPresenceTuning tuning, string filePath)
    {
        if (tuning.RumorBaseChance < 0f)
            throw new DataLoadException(filePath, null, "rumor_base_chance must be >= 0.");
        if (tuning.RumorSameRegionBonus < 0f)
            throw new DataLoadException(filePath, null, "rumor_same_region_bonus must be >= 0.");
        if (tuning.RumorAbsenceBonus < 0f)
            throw new DataLoadException(filePath, null, "rumor_absence_bonus must be >= 0.");
        if (tuning.RumorAbsenceThresholdDays < 0)
            throw new DataLoadException(filePath, null, "rumor_absence_threshold_days must be >= 0.");
        if (tuning.RumorCap < 0f || tuning.RumorCap > 1f)
            throw new DataLoadException(filePath, null, "rumor_cap must be between 0 and 1.");
    }
}

/// <summary>
/// DataRegistry adapter for the singleton comet-presence tuning record.
/// </summary>
public sealed class CometPresenceTuningTable : IDataTable<CometPresenceTuning>
{
    private readonly CometPresenceTuning _tuning;

    public CometPresenceTuningTable(CometPresenceTuning tuning)
    {
        _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
    }

    public CometPresenceTuning? Get(string id)
    {
        return string.Equals(id, "default", StringComparison.Ordinal) ? _tuning : null;
    }

    public IReadOnlyList<CometPresenceTuning> GetAll()
    {
        return new[] { _tuning };
    }

    public int Count => 1;
}

/// <summary>
/// NPC State boundary required by comet-presence tracking.
/// </summary>
public interface IRomanceCometPresencePort
{
    /// <summary>Reads the persisted comet-presence counters for an NPC.</summary>
    CometPresenceCounters? GetCometPresenceCounters(string npcId);

    /// <summary>Reads the heroine journey region used for rumor probability.</summary>
    string? GetJourneyRegion(string npcId);

    /// <summary>Reads the last direct contact day, if any.</summary>
    int? GetLastContactDay(string npcId);

    /// <summary>Records one comet-presence event and optionally updates last contact day.</summary>
    bool RecordCometPresence(string npcId, CometPresenceKind kind, int? lastContactDay, string source);
}

/// <summary>
/// Provides the current world day for deterministic comet-presence tests.
/// </summary>
public interface IWorldDayProvider
{
    /// <summary>Current absolute world day.</summary>
    int CurrentDay { get; }
}

/// <summary>
/// Calculates rumor chance and records comet-presence counters without owning state.
/// </summary>
public sealed class CometPresenceTracker
{
    private const string DefaultSource = "romance_comet";
    private readonly IRomanceCometPresencePort _npcState;
    private readonly IWorldDayProvider _worldDay;
    private readonly CometPresenceTuning _tuning;

    public CometPresenceTracker(
        IRomanceCometPresencePort npcState,
        IWorldDayProvider worldDay,
        CometPresenceTuning tuning)
    {
        _npcState = npcState;
        _worldDay = worldDay;
        _tuning = tuning;
    }

    /// <summary>
    /// Calculates rumor trigger chance as base + same-region bonus + absence bonus, clamped to the configured cap.
    /// </summary>
    public float CalcRumorChance(string npcId, string region)
    {
        var chance = _tuning.RumorBaseChance;
        var journeyRegion = _npcState.GetJourneyRegion(npcId);
        if (!string.IsNullOrWhiteSpace(region)
            && string.Equals(journeyRegion, region, StringComparison.Ordinal))
        {
            chance += _tuning.RumorSameRegionBonus;
        }

        var daysSinceLastContact = GetDaysSinceLastContact(npcId);
        if (daysSinceLastContact != null
            && daysSinceLastContact.Value > _tuning.RumorAbsenceThresholdDays)
        {
            chance += _tuning.RumorAbsenceBonus;
        }

        return Math.Clamp(chance, 0f, _tuning.RumorCap);
    }

    /// <summary>
    /// Computes contact absence from the persisted last-contact day without mutating contact recency.
    /// </summary>
    public int? GetDaysSinceLastContact(string npcId)
    {
        var lastContactDay = _npcState.GetLastContactDay(npcId);
        return lastContactDay == null
            ? null
            : Math.Max(0, _worldDay.CurrentDay - lastContactDay.Value);
    }

    /// <summary>Records a discovered sign and updates the direct-contact day.</summary>
    public bool RecordSignDiscovered(string npcId, string? source = null)
    {
        return Record(npcId, CometPresenceKind.Sign, updateLastContact: true, source);
    }

    /// <summary>Records a received letter and updates the direct-contact day.</summary>
    public bool RecordLetterReceived(string npcId, string? source = null)
    {
        return Record(npcId, CometPresenceKind.Letter, updateLastContact: true, source);
    }

    /// <summary>Records a heard rumor without forcing direct-contact recency.</summary>
    public bool RecordRumorHeard(string npcId, string? source = null)
    {
        return Record(npcId, CometPresenceKind.Rumor, updateLastContact: false, source);
    }

    /// <summary>Records an encounter without forcing direct-contact recency.</summary>
    public bool RecordEncounter(string npcId, string? source = null)
    {
        return Record(npcId, CometPresenceKind.Encounter, updateLastContact: false, source);
    }

    /// <summary>Returns the stable NPC State flag used to continue last-contact day after save/load.</summary>
    public static string GetLastContactFlag(string npcId)
    {
        return $"romance_last_contact_{npcId}";
    }

    private bool Record(string npcId, CometPresenceKind kind, bool updateLastContact, string? source)
    {
        if (string.IsNullOrWhiteSpace(npcId)) return false;

        var contactDay = updateLastContact ? _worldDay.CurrentDay : (int?)null;
        var changeSource = string.IsNullOrWhiteSpace(source) ? DefaultSource : source;
        return _npcState.RecordCometPresence(npcId, kind, contactDay, changeSource);
    }
}
