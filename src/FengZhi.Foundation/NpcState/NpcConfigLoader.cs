using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.NpcState;

/// <summary>
/// NPC 模板 YAML 加载器。集成 DataRegistry。
/// </summary>
public sealed class NpcConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// 从多个 YAML 源加载 NPC 模板并注册到 DataRegistry。
    /// </summary>
    /// <param name="sources">文件名 → YAML 内容</param>
    /// <param name="registry">目标 DataRegistry</param>
    public void LoadAllTemplates(Dictionary<string, string> sources, DataRegistry registry)
    {
        var table = new NpcTemplateTable();

        foreach (var (fileName, yaml) in sources)
        {
            try
            {
                var template = Deserializer.Deserialize<NpcTemplate>(yaml);
                if (string.IsNullOrWhiteSpace(template.Id))
                    throw new DataLoadException(fileName, 1, "NPC 模板缺少 'id' 字段");

                if (!table.TryAdd(template))
                    throw new DataLoadException(fileName, 1, $"NPC 模板 ID 重复: '{template.Id}'");
            }
            catch (DataLoadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataLoadException(fileName, 1, $"YAML 解析失败: {ex.Message}");
            }
        }

        registry.RegisterTable(table);
    }
}

/// <summary>
/// NPC 模板数据表实现。
/// </summary>
public sealed class NpcTemplateTable : IDataTable<NpcTemplate>
{
    private readonly Dictionary<string, NpcTemplate> _templates = new();

    public int Count => _templates.Count;

    public NpcTemplate? Get(string id)
    {
        return _templates.TryGetValue(id, out var t) ? t : null;
    }

    public IReadOnlyList<NpcTemplate> GetAll()
    {
        return _templates.Values.ToList();
    }

    public bool TryAdd(NpcTemplate template)
    {
        return _templates.TryAdd(template.Id, template);
    }
}
