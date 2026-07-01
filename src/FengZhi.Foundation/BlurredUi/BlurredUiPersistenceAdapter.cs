using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.BlurredUi;

public sealed class BlurredUiPersistenceAdapter : ISaveable
{
    private const string DataField = "blurred_ui_data";
    private readonly BlurredUiService _service;

    public BlurredUiPersistenceAdapter(BlurredUiService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public string SaveKey => "blurred_ui";

    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var data = _service.GetSaveData();
        snapshot.Values[DataField] = JsonSerializer.SerializeToElement(data);
        return snapshot;
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue(DataField, out var element))
            return;

        var data = element.Deserialize<BlurredUiSaveData>();
        if (data != null)
            _service.LoadSaveData(data);
    }
}
