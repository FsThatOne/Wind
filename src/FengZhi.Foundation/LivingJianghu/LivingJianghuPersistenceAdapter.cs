using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.LivingJianghu;

public sealed class LivingJianghuPersistenceAdapter : ISaveable
{
    private const string DataField = "living_jianghu_data";
    private readonly LivingJianghuService _service;

    public LivingJianghuPersistenceAdapter(LivingJianghuService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public string SaveKey => "living_jianghu";

    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var data = _service.ExportSaveData();
        snapshot.Values[DataField] = JsonSerializer.SerializeToElement(data);
        return snapshot;
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue(DataField, out var element))
            return;

        var data = element.Deserialize<LivingJianghuSaveData>();
        if (data != null)
            _service.LoadSaveData(data);
    }
}
