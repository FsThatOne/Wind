using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Party;

public sealed class PartyPersistenceAdapter : ISaveable
{
    private const string DataField = "party_data";
    private readonly PartyRoster _roster;

    public PartyPersistenceAdapter(PartyRoster roster)
    {
        _roster = roster ?? throw new ArgumentNullException(nameof(roster));
    }

    public string SaveKey => "party";

    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var data = _roster.ExportSaveData();
        snapshot.Values[DataField] = JsonSerializer.SerializeToElement(data);
        return snapshot;
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue(DataField, out var element))
            return;

        var data = element.Deserialize<PartySaveData>();
        if (data != null)
            _roster.LoadSaveData(data);
    }
}
