using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// Save-system adapter for romance-owned NPC State flags.
/// </summary>
public sealed class RomancePersistenceAdapter : ISaveable
{
    public const string RomanceSaveKey = "romance";
    public const string RomanceFlagsField = "npc_romance_flags";

    private readonly IRomancePersistencePort _persistencePort;

    public RomancePersistenceAdapter(IRomancePersistencePort persistencePort)
    {
        _persistencePort = persistencePort ?? throw new ArgumentNullException(nameof(persistencePort));
    }

    /// <inheritdoc />
    public string SaveKey => RomanceSaveKey;

    /// <inheritdoc />
    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var flags = _persistencePort.ExportRomanceFlags()
            .ToDictionary(
                npc => npc.Key,
                npc => npc.Value.ToDictionary(flag => flag.Key, flag => flag.Value, StringComparer.Ordinal),
                StringComparer.Ordinal);

        snapshot.Values[RomanceFlagsField] = JsonSerializer.SerializeToElement(flags);
        return snapshot;
    }

    /// <inheritdoc />
    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue(RomanceFlagsField, out var element))
        {
            _persistencePort.RestoreRomanceFlags(
                new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal),
                "save_load");
            return;
        }

        var flags = element.Deserialize<Dictionary<string, Dictionary<string, string>>>()
            ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var readOnlyFlags = flags.ToDictionary(
            npc => npc.Key,
            npc => (IReadOnlyDictionary<string, string>)npc.Value,
            StringComparer.Ordinal);

        _persistencePort.RestoreRomanceFlags(readOnlyFlags, "save_load");
    }

    /// <summary>Clears persisted romance runtime state for a new playthrough.</summary>
    public void ResetForNewRun()
    {
        _persistencePort.ResetRomanceForNewRun("new_run");
    }
}
