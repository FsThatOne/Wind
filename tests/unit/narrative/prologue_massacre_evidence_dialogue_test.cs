using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueMassacreEvidenceDialogueTest
{
    private static readonly string[] DialogueFiles =
    {
        "massacre_return_01.yaml",
        "massacre_evidence_01.yaml"
    };

    [Fact]
    public void MassacreReturnDialogue_LoadsAndBuildsSilentTransition()
    {
        var text = ReadDialogueText("massacre_return_01.yaml");

        Assert.Contains("虫鸣", text);
        Assert.Contains("风声", text);
        Assert.Contains("山门", text);
        Assert.Contains("敞", text);
        Assert.Contains("什么也没有", text);
    }

    [Fact]
    public void MassacreEvidenceDialogue_OffersAtLeastFiveActiveInvestigations()
    {
        var sequence = Load("massacre_evidence_01.yaml");
        var choice = Assert.Single(sequence.Nodes, node => node.Id == "choose_evidence");
        var investigationOptions = choice.Options
            .Where(option => option.Next != "evidence_done")
            .ToArray();
        var text = ReadDialogueText("massacre_evidence_01.yaml");

        Assert.True(investigationOptions.Length >= 5);
        Assert.Contains("剑还在架上", text);
        Assert.Contains("庄训", text);
        Assert.Contains("风止", text);
        Assert.Contains("暗格空了", text);
        Assert.Contains("半封血书", text);
        Assert.Contains("尸首里没有她", text);
    }

    [Fact]
    public void MassacreEvidenceDialogue_RecordsRequiredProgressFlags()
    {
        var events = LoadAll()
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => node.Events)
            .ToArray();

        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_massacre_discovered"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_blood_letter_obtained"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_sister_missing_known"
                                     && e.Value == "true");
    }

    [Fact]
    public void MassacreEvidenceDialogue_HidesInvestigatedOptionsAndGatesSummary()
    {
        var sequence = Load("massacre_evidence_01.yaml");
        var choice = Assert.Single(sequence.Nodes, node => node.Id == "choose_evidence");
        var summary = Assert.Single(choice.Options, option => option.Next == "evidence_done");
        var evidenceDone = Assert.Single(sequence.Nodes, node => node.Id == "evidence_done");

        Assert.All(
            choice.Options.Where(option => option.Next != "evidence_done"),
            option => Assert.Contains(option.Conditions, condition => condition.Op == "neq"));
        Assert.Empty(summary.Conditions);
        Assert.True(evidenceDone.Conditions.Count >= 5);
        Assert.Equal("evidence_not_done", evidenceDone.Fallback);
    }

    [Fact]
    public void MassacreEvidenceDialogue_ContainsExactBloodLetterAndKeepsSisterMystery()
    {
        var text = ReadDialogueText("massacre_evidence_01.yaml");

        Assert.Contains("风起渊底，鹤归无枝。", text);
        Assert.Contains("尸首里没有她", text);
        Assert.Contains("不知道她去了哪里", text);

        Assert.DoesNotContain("被掳", text);
        Assert.DoesNotContain("逃脱", text);
        Assert.DoesNotContain("主动离开", text);
    }

    [Fact]
    public void MassacreEvidenceDialogue_DoesNotTriggerCombatOrRevealCulprit()
    {
        var yaml = ReadAllYaml();

        Assert.DoesNotContain("combat_trigger", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tutorial_combat", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("encounter", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("battle", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("追杀战", yaml);
        Assert.DoesNotContain("可击败", yaml);
        Assert.DoesNotContain("沉渊阁", yaml);
        Assert.DoesNotContain("照壁堂", yaml);
        Assert.DoesNotContain("暗令", yaml);
        Assert.DoesNotContain("令牌", yaml);
        Assert.DoesNotContain("门派徽记", yaml);
        Assert.DoesNotContain("凶手是", yaml);
        Assert.DoesNotContain("复仇", yaml);
        Assert.DoesNotContain("血债血偿", yaml);
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

    private static string ReadDialogueText(string fileName)
    {
        var sequence = Load(fileName);
        return string.Join("\n", sequence.Nodes
            .SelectMany(node => new[] { node.Text, node.Prompt })
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string ReadAllYaml()
    {
        return string.Join("\n", DialogueFiles.Select(fileName =>
            File.ReadAllText(Path.Combine(DialogueDirectory(), fileName))));
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
