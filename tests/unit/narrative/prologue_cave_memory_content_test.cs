using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueCaveMemoryContentTest
{
    private static readonly string[] DialogueFiles =
    {
        "wine_pickup_01.yaml",
        "cave_wall_memory_cg_01.yaml",
        "cave_rest_mat_memory_cg_01.yaml",
        "rest_spot_01.yaml",
        "memory_marker_01.yaml",
        "storage_shelf_01.yaml"
    };

    [Fact]
    public void CaveDialogues_LoadAndRecordWineAndOvernightState()
    {
        var sequences = LoadAllDialogues();
        var events = sequences
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => node.Events)
            .ToArray();

        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_wine_obtained"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_cave_wall_memory_seen"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_cave_rest_memory_seen"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_cave_overnight"
                                     && e.Value == "true");
    }

    [Fact]
    public void CaveInsightNodes_AreAllMemoryFocusedEnvironmentDetails()
    {
        var source = ReadBackMountainCaveSource();

        Assert.Contains("cave_wall_technique_sketch", source);
        Assert.Contains("cave_small_stool_memory", source);
        Assert.Contains("cave_wine_stain_pattern", source);
        Assert.Contains("cave_medicine_pot_residue", source);
        Assert.Equal(4, CountOccurrences(source, "DiscoveryType.EnvironmentDetail"));
        Assert.DoesNotContain("DiscoveryType.Clue", source);
        Assert.DoesNotContain("cave_loose_brick", source);
        Assert.DoesNotContain("ch00_loose_brick", source);
    }

    [Fact]
    public void CaveMemoryContent_CoversFourSpecificSisterMemories()
    {
        var text = ReadBackMountainCaveSource() + "\n" + ReadAllDialogueText();

        Assert.Contains("招式刻画", text);
        Assert.Contains("风止尺法", text);
        Assert.Contains("封坛", text);
        Assert.Contains("草席", text);
        Assert.Contains("小木凳", text);
        Assert.Contains("药壶", text);
        Assert.Contains("师姐", text);
    }

    [Fact]
    public void CaveMemoryContent_DoesNotShiftMindsetInPrologue()
    {
        Assert.DoesNotContain("mindset_shift", ReadAllYaml());
    }

    [Fact]
    public void CaveOvernightReason_ComesFromNightRainAndSlipperyPath()
    {
        var text = ReadDialogueText("rest_spot_01.yaml");

        Assert.Contains("山雨", text);
        Assert.Contains("天黑路险", text);
        Assert.Contains("石阶", text);
        Assert.Contains("滑", text);
        Assert.DoesNotContain("敌人", text);
        Assert.DoesNotContain("阻拦", text);
    }

    [Fact]
    public void CaveMemoryContent_DoesNotPolluteWithConspiracyOrRewardSignals()
    {
        var content = ReadBackMountainCaveSource() + "\n" + ReadAllYaml();

        Assert.DoesNotContain("松动", content);
        Assert.DoesNotContain("砖缝", content);
        Assert.DoesNotContain("暗格", content);
        Assert.DoesNotContain("暗记", content);
        Assert.DoesNotContain("旧纸屑", content);
        Assert.DoesNotContain("凶手", content);
        Assert.DoesNotContain("外人", content);
        Assert.DoesNotContain("暗令", content);
        Assert.DoesNotContain("门派徽记", content);
        Assert.DoesNotContain("CodePhrase", content);
        Assert.DoesNotContain("MartialFragment", content);
        Assert.DoesNotContain("Loot", content);
    }

    private static DialogueSequence[] LoadAllDialogues()
    {
        var loader = new DialogueConfigLoader();
        return DialogueFiles
            .Select(fileName =>
            {
                var path = Path.Combine(DialogueDirectory(), fileName);
                return loader.LoadSequence(File.ReadAllText(path), path);
            })
            .ToArray();
    }

    private static string ReadAllDialogueText()
    {
        return string.Join("\n", LoadAllDialogues()
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => new[] { node.Text, node.Prompt })
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string ReadDialogueText(string fileName)
    {
        var sequence = LoadAllDialogues().Single(sequence => sequence.Id == Path.GetFileNameWithoutExtension(fileName));
        return string.Join("\n", sequence.Nodes
            .SelectMany(node => new[] { node.Text, node.Prompt })
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string ReadAllYaml()
    {
        return string.Join("\n", DialogueFiles.Select(fileName =>
            File.ReadAllText(Path.Combine(DialogueDirectory(), fileName))));
    }

    private static string ReadBackMountainCaveSource()
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "feng-zhi", "scripts", "BackMountainCliffCaveGame.cs"));
    }

    private static string DialogueDirectory()
    {
        return Path.Combine(FindRepositoryRoot(), "feng-zhi", "assets", "data", "dialogues", "chapter_00");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
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
