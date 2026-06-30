using FengZhi.Foundation.Narrative;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class Chapter00MainlineGraphTest
{
    private static readonly string[] ExpectedNodeIds =
    {
        "p00_01_wake_in_manor",
        "p00_02_sister_gather_herb",
        "p00_03_first_ore_and_tracks",
        "p00_04_manor_errands",
        "p00_05_master_study",
        "p00_06_delay_until_dusk",
        "p00_07_cliff_cave_wine",
        "p00_08_silent_return",
        "p00_09_massacre_evidence",
        "p00_10_senior_brother_farewell"
    };

    private static readonly string[] RequiredStateKeys =
    {
        "prologue_gathering_taught",
        "prologue_mount_foreshadowed",
        "prologue_wine_delayed",
        "prologue_wine_obtained",
        "prologue_massacre_discovered",
        "prologue_blood_letter_obtained",
        "prologue_sister_missing_known",
        "mis_senior_brother_survivor_suspicion",
        "senior_brother_mis_resolved",
        "senior_brother_letter_contact_unlocked"
    };

    [Fact]
    public void Chapter00Yaml_LoadsWithTenOrderedPrologueNodes()
    {
        var graph = LoadChapter00Graph();

        Assert.Equal("ch00_prologue", graph.Id);
        Assert.Equal("p00_01_wake_in_manor", graph.EntryNode);
        Assert.Equal(ExpectedNodeIds, graph.Nodes.Select(node => node.Id).ToArray());
        Assert.Equal(10, graph.Nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(graph.Nodes, node => Assert.False(string.IsNullOrWhiteSpace(node.Title)));
        Assert.All(graph.Nodes, node => Assert.NotEmpty(node.OnComplete));
    }

    [Fact]
    public void Chapter00Yaml_ChainsNodesThroughApprovedOneDayOneNightStructure()
    {
        var graph = LoadChapter00Graph();
        var nodes = graph.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        for (var i = 1; i < ExpectedNodeIds.Length; i++)
        {
            var node = nodes[ExpectedNodeIds[i]];
            Assert.Contains(node.Preconditions, condition =>
                condition.Kind == NarrativeConditionKind.NodeCompleted &&
                condition.Key == ExpectedNodeIds[i - 1]);
        }

        Assert.Equal("p00_02_sister_gather_herb", nodes["p00_01_wake_in_manor"].Next);
        Assert.Equal("p00_03_first_ore_and_tracks", nodes["p00_02_sister_gather_herb"].Next);
        Assert.Equal("p00_04_manor_errands", nodes["p00_03_first_ore_and_tracks"].Next);
        Assert.Equal("p00_05_master_study", Assert.Single(nodes["p00_04_manor_errands"].Branches, b => b.Id == "kitchen_route").Next);
        Assert.Equal("p00_07_cliff_cave_wine", nodes["p00_06_delay_until_dusk"].Next);
        Assert.Equal("p00_10_senior_brother_farewell", nodes["p00_09_massacre_evidence"].Next);
    }

    [Fact]
    public void Chapter00Yaml_DefinesRequiredStoryStateKeys()
    {
        var graph = LoadChapter00Graph();
        var eventKeys = graph.Nodes
            .SelectMany(node => node.OnComplete)
            .Select(spec => spec.Key)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var key in RequiredStateKeys)
            Assert.Contains(key, eventKeys);
    }

    [Fact]
    public void Chapter00Yaml_ExcludesCombatAndConspiracySignalsBeforeFarewell()
    {
        var graph = LoadChapter00Graph();
        var beforeFarewell = graph.Nodes.TakeWhile(node => node.Id != "p00_10_senior_brother_farewell").ToArray();

        Assert.DoesNotContain(beforeFarewell, node => node.Type == NarrativeNodeType.Combat);

        var yaml = ReadChapter00Yaml();
        Assert.DoesNotContain("tutorial_combat", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("外人脚印", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("凶手线索", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("师姐欲言又止", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Chapter00Yaml_MarksSeniorBrotherDependenciesWithoutBlockingEarlierNodes()
    {
        var graph = LoadChapter00Graph();
        var farewell = Assert.Single(graph.Nodes, node => node.Id == "p00_10_senior_brother_farewell");

        Assert.Equal(NarrativeNodeType.Dialogue, farewell.Type);
        Assert.Contains(farewell.OnComplete, spec => spec.Type == "register_misunderstanding"
                                                     && spec.Key == "mis_senior_brother_survivor_suspicion");

        var yaml = ReadChapter00Yaml();
        Assert.Contains("misunderstanding-system", yaml, StringComparison.Ordinal);
        Assert.Contains("combat-tutorial", yaml, StringComparison.Ordinal);
    }

    private static NarrativeGraph LoadChapter00Graph()
    {
        return new NarrativeGraphLoader().LoadFromYaml(ReadChapter00Yaml());
    }

    private static string ReadChapter00Yaml()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "assets", "data", "narrative", "chapter_00.yaml"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FengZhi.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("无法定位 FengZhi.slnx 所在的仓库根目录。");
    }
}
