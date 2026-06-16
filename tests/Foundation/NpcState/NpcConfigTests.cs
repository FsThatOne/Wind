using Xunit;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Data;

namespace FengZhi.Tests.Foundation.NpcStates;

public class NpcConfigTests
{
    private const string ValidYaml = @"
id: bai_ling
name: 白灵
initial_life: Alive
initial_presence: NearbyVisible
initial_location: Town
initial_interaction: DialogueAvailable
initial_relationship: Stranger
base_attitude: 0
mindset_compatibility:
  release: 1
  obsession: -1
morality_reaction:
  honorable: 1
  ruthless: -1
delegate_allowed: true
help_request_allowed: true
journey_required_chapter: 3
journey_required_days: 15
";

    private const string MinimalYaml = @"
id: lao_zhang
name: 老张
";

    // ─── AC1: YAML 反序列化正确 ─────────────────────────────

    [Fact]
    public void LoadTemplate_ValidYaml_DeserializesCorrectly()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();
        loader.LoadAllTemplates(new() { ["npcs/bai_ling.yaml"] = ValidYaml }, registry);

        var table = registry.GetTable<NpcTemplate>();
        Assert.NotNull(table);
        var template = table!.Get("bai_ling");
        Assert.NotNull(template);
        Assert.Equal("白灵", template!.Name);
        Assert.Equal("Alive", template.InitialLife);
        Assert.Equal("NearbyVisible", template.InitialPresence);
        Assert.Equal("Town", template.InitialLocation);
        Assert.Equal("DialogueAvailable", template.InitialInteraction);
        Assert.Equal(0, template.BaseAttitude);
        Assert.True(template.DelegateAllowed);
        Assert.True(template.HelpRequestAllowed);
        Assert.Equal(3, template.JourneyRequiredChapter);
        Assert.Equal(15, template.JourneyRequiredDays);
    }

    // ─── AC2: DataRegistry 正常查询 ─────────────────────────

    [Fact]
    public void GetTable_Get_ReturnsValidObject()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();
        loader.LoadAllTemplates(new() { ["npcs/bai_ling.yaml"] = ValidYaml }, registry);

        var table = registry.GetTable<NpcTemplate>();
        var result = table!.Get("bai_ling");
        Assert.NotNull(result);
        Assert.Equal("bai_ling", result!.Id);
    }

    // ─── AC3: 态度配置表包含 mindset 和 morality 映射 ───────

    [Fact]
    public void Template_MindsetCompatibility_Loaded()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();
        loader.LoadAllTemplates(new() { ["npcs/bai_ling.yaml"] = ValidYaml }, registry);

        var template = registry.GetTable<NpcTemplate>()!.Get("bai_ling")!;
        Assert.Equal(1, template.MindsetCompatibility["release"]);
        Assert.Equal(-1, template.MindsetCompatibility["obsession"]);
    }

    [Fact]
    public void Template_MoralityReaction_Loaded()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();
        loader.LoadAllTemplates(new() { ["npcs/bai_ling.yaml"] = ValidYaml }, registry);

        var template = registry.GetTable<NpcTemplate>()!.Get("bai_ling")!;
        Assert.Equal(1, template.MoralityReaction["honorable"]);
        Assert.Equal(-1, template.MoralityReaction["ruthless"]);
    }

    // ─── AC4: 格式错误 YAML → DataLoadException ─────────────

    [Fact]
    public void LoadTemplate_InvalidYaml_ThrowsDataLoadException()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();

        var ex = Assert.Throws<DataLoadException>(() =>
            loader.LoadAllTemplates(new() { ["bad.yaml"] = "{{{{invalid yaml" }, registry));

        Assert.Contains("bad.yaml", ex.FilePath);
        Assert.Contains("YAML 解析失败", ex.Message);
    }

    [Fact]
    public void LoadTemplate_MissingId_ThrowsDataLoadException()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();

        var ex = Assert.Throws<DataLoadException>(() =>
            loader.LoadAllTemplates(new() { ["no_id.yaml"] = "name: test\n" }, registry));

        Assert.Contains("缺少 'id' 字段", ex.Message);
    }

    // ─── AC5: 同 ID 重复 → 报错 ────────────────────────────

    [Fact]
    public void LoadTemplate_DuplicateId_ThrowsDataLoadException()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();

        var ex = Assert.Throws<DataLoadException>(() =>
            loader.LoadAllTemplates(new()
            {
                ["a.yaml"] = ValidYaml,
                ["b.yaml"] = ValidYaml // 同 ID bai_ling
            }, registry));

        Assert.Contains("ID 重复", ex.Message);
    }

    // ─── AC6: 缺少可选字段时使用安全默认值 ──────────────────

    [Fact]
    public void LoadTemplate_MinimalYaml_DefaultValues()
    {
        var registry = new DataRegistry();
        var loader = new NpcConfigLoader();
        loader.LoadAllTemplates(new() { ["npcs/lao_zhang.yaml"] = MinimalYaml }, registry);

        var template = registry.GetTable<NpcTemplate>()!.Get("lao_zhang")!;
        Assert.Equal("老张", template.Name);
        // 默认值
        Assert.Equal("Unknown", template.InitialLife);
        Assert.Equal("Unreachable", template.InitialPresence);
        Assert.Equal(0, template.BaseAttitude);
        Assert.False(template.DelegateAllowed);
        Assert.False(template.HelpRequestAllowed);
        Assert.Equal(-1, template.JourneyRequiredChapter);
        Assert.Empty(template.MindsetCompatibility);
        Assert.Empty(template.MoralityReaction);
    }
}
