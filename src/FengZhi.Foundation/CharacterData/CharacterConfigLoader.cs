using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// YAML 角色配置加载器。
/// 启动时扫描指定目录，加载所有 .yaml 文件并注册到 DataRegistry。
/// 格式错误立即抛出 DataLoadException（含文件名+行号）。
/// </summary>
public sealed class CharacterConfigLoader
{
    private readonly IDeserializer _deserializer;

    public CharacterConfigLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 从 YAML 字符串加载单个 CharacterTemplate。
    /// </summary>
    public CharacterTemplate LoadTemplate(string yaml, string filePath)
    {
        try
        {
            var template = _deserializer.Deserialize<CharacterTemplate>(yaml);
            if (template == null)
                throw new DataLoadException(filePath, null, "YAML 反序列化结果为 null");
            return template;
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, ex.Message, ex);
        }
    }

    /// <summary>
    /// 从 YAML 字符串加载 AttributeTuningConfig。
    /// </summary>
    public AttributeTuningConfig LoadTuning(string yaml, string filePath)
    {
        try
        {
            var config = _deserializer.Deserialize<AttributeTuningConfig>(yaml);
            if (config == null)
                throw new DataLoadException(filePath, null, "YAML 反序列化结果为 null");
            return config;
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, ex.Message, ex);
        }
    }

    /// <summary>
    /// 从多个 YAML 字符串批量加载角色模板，注册到 DataRegistry。
    /// 同 ID 冲突时立即报错。
    /// </summary>
    /// <param name="yamlSources">键为文件路径，值为 YAML 内容</param>
    /// <param name="registry">数据注册中心</param>
    public void LoadAllTemplates(IReadOnlyDictionary<string, string> yamlSources, DataRegistry registry)
    {
        var table = new CharacterTemplateTable();

        foreach (var (filePath, yaml) in yamlSources)
        {
            var template = LoadTemplate(yaml, filePath);

            if (string.IsNullOrWhiteSpace(template.Id))
                throw new DataLoadException(filePath, null, "缺少必需字段 'id'");

            if (!table.TryAdd(template))
                throw new DataLoadException(filePath, null, $"角色模板 ID 重复: '{template.Id}'");
        }

        registry.RegisterTable<CharacterTemplate>(table);
    }
}

/// <summary>
/// CharacterTemplate 的内存数据表实现。
/// </summary>
public sealed class CharacterTemplateTable : IDataTable<CharacterTemplate>
{
    private readonly Dictionary<string, CharacterTemplate> _map = new();
    private readonly List<CharacterTemplate> _list = new();

    public int Count => _list.Count;

    public CharacterTemplate? Get(string id)
    {
        return _map.TryGetValue(id, out var t) ? t : null;
    }

    public IReadOnlyList<CharacterTemplate> GetAll() => _list;

    /// <summary>尝试添加模板。ID 冲突时返回 false。</summary>
    internal bool TryAdd(CharacterTemplate template)
    {
        if (_map.ContainsKey(template.Id))
            return false;
        _map[template.Id] = template;
        _list.Add(template);
        return true;
    }
}
