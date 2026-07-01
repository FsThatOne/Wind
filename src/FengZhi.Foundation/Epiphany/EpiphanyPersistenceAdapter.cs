using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanyPersistenceAdapter : ISaveable
{
    private const string DataField = "epiphany_data";
    private readonly EpiphanyRegistry _registry;

    public EpiphanyPersistenceAdapter(EpiphanyRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public string SaveKey => "epiphany";

    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var data = _registry.ExportState();
        snapshot.Values[DataField] = JsonSerializer.SerializeToElement(data);
        return snapshot;
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue(DataField, out var element))
            return;

        var data = element.Deserialize<EpiphanySaveData>();
        if (data != null)
            _registry.LoadState(data);
    }
}
