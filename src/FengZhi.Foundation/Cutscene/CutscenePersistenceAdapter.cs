using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Cutscene;

/// <summary>
/// ICutsceneViewedStore 实现 + ISaveable 适配器。既追踪已观看过场动画，也负责持久化。
/// </summary>
public sealed class CutscenePersistenceAdapter : ICutsceneViewedStore, ISaveable
{
    private const string ViewedField = "viewed_ids";
    private readonly HashSet<string> _viewed = new(StringComparer.Ordinal);

    public string SaveKey => "cutscene_viewed";

    public bool HasViewed(string scriptId) => _viewed.Contains(scriptId);

    public void MarkViewed(string scriptId)
    {
        if (!string.IsNullOrWhiteSpace(scriptId))
            _viewed.Add(scriptId);
    }

    public HashSet<string> GetAllViewed() => new(_viewed, StringComparer.Ordinal);

    public void RestoreViewed(HashSet<string> viewedIds)
    {
        _viewed.Clear();
        if (viewedIds != null)
        {
            foreach (var id in viewedIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _viewed.Add(id);
            }
        }
    }

    public SaveSnapshot Serialize()
    {
        var snapshot = new SaveSnapshot();
        var sorted = _viewed.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        snapshot.Values[ViewedField] = JsonSerializer.SerializeToElement(sorted);
        return snapshot;
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        _viewed.Clear();

        if (!snapshot.Values.TryGetValue(ViewedField, out var element))
            return;

        var ids = element.Deserialize<string[]>();
        if (ids == null) return;

        foreach (var id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
                _viewed.Add(id);
        }
    }
}
