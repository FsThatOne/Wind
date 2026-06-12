using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 武学配置加载器。加载招式、心法、轻功和刚柔巧克制矩阵并注册到 DataRegistry。
/// </summary>
public sealed class MartialArtsConfigLoader
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    /// <summary>
    /// 加载并校验全部武学 YAML 配置，注册四张数据表到 <see cref="DataRegistry"/>。
    /// 任何解析或校验错误抛出 <see cref="DataLoadException"/>（快速失败）。
    /// </summary>
    public void LoadAll(
        string movesYaml,
        string xinfaYaml,
        string qinggongYaml,
        string counterMatrixYaml,
        DataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var moves = LoadList<MoveDefinition>(movesYaml, "assets/data/martial-arts/moves.yaml");
        var xinfa = LoadList<XinfaDefinition>(xinfaYaml, "assets/data/martial-arts/xinfa.yaml");
        var qinggong = LoadList<QinggongDefinition>(qinggongYaml, "assets/data/martial-arts/qinggong.yaml");
        var counterEntries = LoadList<CounterMatrixEntry>(counterMatrixYaml, "assets/data/martial-arts/counter-matrix.yaml");

        ValidateMoves(moves, "assets/data/martial-arts/moves.yaml");
        ValidateXinfa(xinfa, moves, "assets/data/martial-arts/xinfa.yaml");
        ValidateQinggong(qinggong, "assets/data/martial-arts/qinggong.yaml");
        ValidateCounterMatrix(counterEntries, "assets/data/martial-arts/counter-matrix.yaml");

        registry.RegisterTable<MoveDefinition>(new MoveDefinitionTable(moves));
        registry.RegisterTable<XinfaDefinition>(new XinfaDefinitionTable(xinfa));
        registry.RegisterTable<QinggongDefinition>(new QinggongDefinitionTable(qinggong));
        registry.RegisterTable<CounterMatrixEntry>(new CounterMatrixTable(counterEntries));
    }

    /// <summary>
    /// 反序列化单个 YAML 列表文档。解析失败时抛出含 <paramref name="filePath"/> 与行号的 <see cref="DataLoadException"/>。
    /// </summary>
    public IReadOnlyList<T> LoadList<T>(string yamlContent, string filePath) where T : class
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return Array.Empty<T>();

        try
        {
            return _deserializer.Deserialize<List<T>>(yamlContent) ?? new List<T>();
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, $"YAML parse error: {ex.Message}", ex);
        }
    }

    private static void ValidateMoves(IReadOnlyList<MoveDefinition> moves, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var move in moves)
        {
            RequireId(move.Id, filePath, "move.id");
            RequireText(move.Name, filePath, $"move[{move.Id}].name");

            if (!ids.Add(move.Id))
                throw new DataLoadException(filePath, null, $"字段 id 重复: '{move.Id}'");

            // 字符串非法枚举在反序列化阶段已抛错；此处仅兜底 YAML 中直接写数字导致的越界值
            if (!Enum.IsDefined(move.Type))
                throw new DataLoadException(filePath, null, $"字段 type 非法: '{move.Type}'");

            if (!Enum.IsDefined(move.Category))
                throw new DataLoadException(filePath, null, $"字段 category 非法: '{move.Category}'");

            if (move.NeixiCost < 0)
                throw new DataLoadException(filePath, null, $"字段 neixi_cost 非法: '{move.Id}' 必须 >= 0");

            if (move.BaseMultiplier <= 0)
                throw new DataLoadException(filePath, null, $"字段 base_multiplier 非法: '{move.Id}' 必须 > 0");

            if (move.TriggerConditions.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 trigger_conditions 缺失: '{move.Id}'");

            if (move.SpecialEffects.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 special_effects 缺失: '{move.Id}'");

            if (move.Tags.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 tags 缺失: '{move.Id}'");
        }
    }

    private static void ValidateXinfa(
        IReadOnlyList<XinfaDefinition> xinfa,
        IReadOnlyList<MoveDefinition> moves,
        string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var moveIds = moves.Select(move => move.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var item in xinfa)
        {
            RequireId(item.Id, filePath, "xinfa.id");
            RequireText(item.Name, filePath, $"xinfa[{item.Id}].name");

            if (!ids.Add(item.Id))
                throw new DataLoadException(filePath, null, $"字段 id 重复: '{item.Id}'");

            foreach (var moveId in item.ExclusiveMoves)
            {
                if (!moveIds.Contains(moveId))
                    throw new DataLoadException(
                        filePath,
                        null,
                        $"字段 exclusive_moves 引用不存在: 心法 '{item.Id}' -> 招式 '{moveId}'");
            }
        }
    }

    private static void ValidateQinggong(IReadOnlyList<QinggongDefinition> qinggong, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in qinggong)
        {
            RequireId(item.Id, filePath, "qinggong.id");
            RequireText(item.Name, filePath, $"qinggong[{item.Id}].name");

            if (!ids.Add(item.Id))
                throw new DataLoadException(filePath, null, $"字段 id 重复: '{item.Id}'");
        }
    }

    private static void ValidateCounterMatrix(IReadOnlyList<CounterMatrixEntry> entries, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var expected = new HashSet<string>(
            from attacker in Enum.GetValues<MoveType>()
            from defender in Enum.GetValues<MoveType>()
            select CounterMatrixEntry.MakeId(attacker, defender),
            StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (!Enum.IsDefined(entry.Attacker))
                throw new DataLoadException(filePath, null, $"字段 attacker 非法: '{entry.Attacker}'");

            if (!Enum.IsDefined(entry.Defender))
                throw new DataLoadException(filePath, null, $"字段 defender 非法: '{entry.Defender}'");

            if (!Enum.IsDefined(entry.Relation))
                throw new DataLoadException(filePath, null, $"字段 relation 非法: '{entry.Relation}'");

            if (entry.Multiplier <= 0)
                throw new DataLoadException(filePath, null, $"字段 multiplier 非法: '{entry.Id}' 必须 > 0");

            if (!ids.Add(entry.Id))
                throw new DataLoadException(filePath, null, $"克制矩阵 key 重复: '{entry.Id}'");
        }

        var missing = expected.Except(ids).ToList();
        if (missing.Count > 0)
            throw new DataLoadException(filePath, null, $"克制矩阵缺少 key: '{missing[0]}'");
    }

    private static void RequireId(string value, string filePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DataLoadException(filePath, null, $"缺少必填字段 {fieldName}");
    }

    private static void RequireText(string value, string filePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DataLoadException(filePath, null, $"缺少必填字段 {fieldName}");
    }
}

/// <summary>招式定义表。按 id O(1) 查询。</summary>
public sealed class MoveDefinitionTable : IDataTable<MoveDefinition>
{
    private readonly Dictionary<string, MoveDefinition> _map;
    private readonly List<MoveDefinition> _list;

    public MoveDefinitionTable(IEnumerable<MoveDefinition> moves)
    {
        _list = moves.ToList();
        _map = _list.ToDictionary(move => move.Id, StringComparer.Ordinal);
    }

    public int Count => _list.Count;

    public MoveDefinition? Get(string id) => _map.TryGetValue(id, out var move) ? move : null;

    public IReadOnlyList<MoveDefinition> GetAll() => _list;
}

/// <summary>心法定义表。按 id O(1) 查询。</summary>
public sealed class XinfaDefinitionTable : IDataTable<XinfaDefinition>
{
    private readonly Dictionary<string, XinfaDefinition> _map;
    private readonly List<XinfaDefinition> _list;

    public XinfaDefinitionTable(IEnumerable<XinfaDefinition> xinfa)
    {
        _list = xinfa.ToList();
        _map = _list.ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    public int Count => _list.Count;

    public XinfaDefinition? Get(string id) => _map.TryGetValue(id, out var item) ? item : null;

    public IReadOnlyList<XinfaDefinition> GetAll() => _list;
}

/// <summary>轻功定义表。按 id O(1) 查询。</summary>
public sealed class QinggongDefinitionTable : IDataTable<QinggongDefinition>
{
    private readonly Dictionary<string, QinggongDefinition> _map;
    private readonly List<QinggongDefinition> _list;

    public QinggongDefinitionTable(IEnumerable<QinggongDefinition> qinggong)
    {
        _list = qinggong.ToList();
        _map = _list.ToDictionary(item => item.Id, StringComparer.Ordinal);
    }

    public int Count => _list.Count;

    public QinggongDefinition? Get(string id) => _map.TryGetValue(id, out var item) ? item : null;

    public IReadOnlyList<QinggongDefinition> GetAll() => _list;
}

/// <summary>刚柔巧克制矩阵表。支持按 (攻击体系, 防守体系) 查询克制关系。</summary>
public sealed class CounterMatrixTable : IDataTable<CounterMatrixEntry>
{
    private readonly Dictionary<string, CounterMatrixEntry> _map;
    private readonly List<CounterMatrixEntry> _list;

    public CounterMatrixTable(IEnumerable<CounterMatrixEntry> entries)
    {
        _list = entries.ToList();
        _map = _list.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
    }

    public int Count => _list.Count;

    public CounterMatrixEntry? Get(string id) => _map.TryGetValue(id, out var entry) ? entry : null;

    public IReadOnlyList<CounterMatrixEntry> GetAll() => _list;

    /// <summary>查询指定攻防体系组合的克制关系记录。</summary>
    public CounterMatrixEntry? Get(MoveType attacker, MoveType defender) =>
        Get(CounterMatrixEntry.MakeId(attacker, defender));
}
