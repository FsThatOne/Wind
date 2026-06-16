using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.Narrative;

public sealed class NarrativeGraphLoader
{
    private readonly IDeserializer _deserializer;

    public NarrativeGraphLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public NarrativeGraph LoadFromYaml(string yaml)
    {
        var graph = _deserializer.Deserialize<NarrativeGraph>(yaml)
            ?? throw new NarrativeGraphValidationException(new[] { "无法解析主线节点图 YAML。" });

        NarrativeGraphValidator.ThrowIfInvalid(graph);
        return graph;
    }
}
