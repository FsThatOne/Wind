using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueManorErrandsDialogueTest
{
    private static readonly string[] DialogueFiles =
    {
        "manor_errands_01.yaml",
        "master_study_01.yaml",
        "sister_wine_reminder_01.yaml"
    };

    [Fact]
    public void ManorErrandDialogues_LoadAndCoverRequiredNpcMemoryPoints()
    {
        var sequences = LoadAll();
        var speakers = sequences
            .SelectMany(sequence => sequence.Nodes)
            .Select(node => node.Speaker)
            .Where(speaker => !string.IsNullOrWhiteSpace(speaker))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("bai_tan", speakers);
        Assert.Contains("manor_master", speakers);
        Assert.Contains("kitchen_disciple", speakers);
        Assert.Contains("junior_brother", speakers);
        Assert.Contains("pharmacy_disciple", speakers);
    }

    [Fact]
    public void ManorErrandDialogues_WalkThroughAtLeastThreeDailyAreas()
    {
        var text = ReadAllDialogueText();

        Assert.Contains("厨房", text);
        Assert.Contains("药房", text);
        Assert.Contains("练武场", text);
        Assert.Contains("正堂", text);
    }

    [Fact]
    public void WineReminderDialogue_GentlyEscalatesAtLeastTwiceWithoutPunishment()
    {
        var sequence = Load("sister_wine_reminder_01.yaml");
        var baiTanLines = sequence.Nodes
            .Where(node => node.Speaker == "bai_tan")
            .Select(node => node.Text ?? string.Empty)
            .ToArray();
        var text = string.Join("\n", baiTanLines);
        var allText = ReadAllDialogueText();

        Assert.True(baiTanLines.Length >= 2);
        Assert.Contains("记得去取", text);
        Assert.Contains("天色要低了", text);
        Assert.Contains("路熟", allText);
        Assert.Contains("再帮一会儿就去", allText);

        Assert.DoesNotContain("倒计时", allText);
        Assert.DoesNotContain("失败", allText);
        Assert.DoesNotContain("扣除", allText);
        Assert.DoesNotContain("game over", allText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MasterStudyDialogue_ForeshadowsHiddenCompartmentWithoutRevealingSecret()
    {
        var text = ReadDialogueText("master_study_01.yaml");
        var yaml = ReadYaml("master_study_01.yaml");

        Assert.Contains("旧灯下面", text);
        Assert.Contains("接缝", text);
        Assert.Contains("prologue_study_hidden_compartment_seen", yaml);

        Assert.DoesNotContain("身世", text);
        Assert.DoesNotContain("文书", text);
        Assert.DoesNotContain("凶手", text);
        Assert.DoesNotContain("澜国", text);
    }

    [Fact]
    public void ManorErrandDialogues_RecordDelayedWineAndExcludeCombatTeaching()
    {
        var yaml = ReadAllYaml();

        Assert.Contains("prologue_wine_delayed", yaml);
        Assert.DoesNotContain("combat_trigger", yaml);
        Assert.DoesNotContain("tutorial_combat", yaml);
        Assert.DoesNotContain("战斗教学", yaml);
        Assert.DoesNotContain("切磋", yaml);
        Assert.DoesNotContain("练功", yaml);
    }

    [Fact]
    public void ManorErrandDialogue_OnlyAcceptsTaskAndDoesNotCompleteSubtasksInPlace()
    {
        var sequence = Load("manor_errands_01.yaml");
        var choice = Assert.Single(sequence.Nodes, node => node.Id == "choose_route");

        Assert.Equal(3, choice.Options.Count);
        Assert.All(
            new[] { "kitchen_hint", "pharmacy_hint", "training_ground_hint" },
            nodeId => Assert.Equal("task_accept", Assert.Single(sequence.Nodes, node => node.Id == nodeId).Next));

        var yaml = ReadYaml("manor_errands_01.yaml");
        Assert.Contains("prologue_manor_errands_started", yaml);
        Assert.DoesNotContain("manor_errand_kitchen_done", yaml);
        Assert.DoesNotContain("manor_errand_pharmacy_done", yaml);
        Assert.DoesNotContain("manor_errand_junior_done", yaml);
        Assert.DoesNotContain("prologue_manor_errands_completed", yaml);
    }

    private static DialogueSequence[] LoadAll()
    {
        return DialogueFiles.Select(Load).ToArray();
    }

    private static DialogueSequence Load(string fileName)
    {
        var path = Path.Combine(DialogueDirectory(), fileName);
        return new DialogueConfigLoader().LoadSequence(File.ReadAllText(path), path);
    }

    private static string ReadAllDialogueText()
    {
        return string.Join("\n", LoadAll()
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => new[] { node.Text, node.Prompt })
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string ReadDialogueText(string fileName)
    {
        var sequence = Load(fileName);
        return string.Join("\n", sequence.Nodes
            .SelectMany(node => new[] { node.Text, node.Prompt })
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string ReadAllYaml()
    {
        return string.Join("\n", DialogueFiles.Select(ReadYaml));
    }

    private static string ReadYaml(string fileName)
    {
        return File.ReadAllText(Path.Combine(DialogueDirectory(), fileName));
    }

    private static string DialogueDirectory()
    {
        return Path.Combine(FindRepositoryRoot(), "feng-zhi", "assets", "data", "dialogues", "chapter_00");
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
