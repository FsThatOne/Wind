using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueSeniorBrotherFarewellContentTest
{
    private static readonly string[] DialogueFiles =
    {
        "senior_brother_return_01.yaml",
        "senior_brother_misunderstanding_01.yaml",
        "joint_burial_01.yaml",
        "farewell_inheritance_01.yaml",
        "letter_promise_01.yaml"
    };

    [Fact]
    public void SeniorBrotherDialogues_LoadAndRegisterSurvivorMisunderstanding()
    {
        var sequences = LoadAll();
        var events = AllEvents(sequences);
        var text = ReadAllDialogueText();

        Assert.Equal(5, sequences.Length);
        Assert.Contains("为什么只有你活着", text);
        Assert.Contains(events, e => e.Type == "register_misunderstanding"
                                     && e.Key == "mis_senior_brother_survivor_suspicion"
                                     && e.Value == "active");
    }

    [Fact]
    public void MisunderstandingSource_StaysOnOnlySurvivorNotIdentity()
    {
        var text = ReadDialogueText("senior_brother_return_01.yaml") + "\n"
                   + ReadDialogueText("senior_brother_misunderstanding_01.yaml");

        Assert.Contains("偏偏你", text);
        Assert.Contains("一个人活着", text);
        Assert.DoesNotContain("澜国", text);
        Assert.DoesNotContain("旧玉", text);
        Assert.DoesNotContain("身世", text);
        Assert.DoesNotContain("异族", text);
    }

    [Fact]
    public void ClarificationReferencesWineCaveBloodLetterAndMissingSister()
    {
        var sequence = Load("senior_brother_misunderstanding_01.yaml");
        var text = ReadDialogueText("senior_brother_misunderstanding_01.yaml");
        var events = AllEvents(new[] { sequence });

        Assert.Contains("取三年陈", text);
        Assert.Contains("崖洞", text);
        Assert.Contains("夜宿", text);
        Assert.Contains("血书", text);
        Assert.Contains("风起渊底，鹤归无枝", text);
        Assert.Contains("师姐不在尸首里", text);
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "senior_brother_mis_resolved"
                                     && e.Value == "true");
    }

    [Fact]
    public void MisunderstandingDialogue_UsesRuntimeModForTransparencySignal()
    {
        var sequence = Load("senior_brother_misunderstanding_01.yaml");
        var signalNode = Assert.Single(sequence.Nodes, node => node.Id == "suspicion_signal");
        var condition = Assert.Single(signalNode.Conditions);

        Assert.Equal("misunderstanding_mod.senior_brother", condition.Source);
        Assert.Equal("lte", condition.Op);
        Assert.Equal("-1", condition.Value);
        Assert.Equal("clarify_choice", signalNode.Fallback);
        Assert.Contains("称呼退回陌生处", signalNode.Text);
    }

    [Fact]
    public void MisunderstandingDialogue_ShowsResolutionReliefBeforeSettingResolvedFlag()
    {
        var sequence = Load("senior_brother_misunderstanding_01.yaml");
        var resolved = Assert.Single(sequence.Nodes, node => node.Id == "resolved_01");
        var relief = Assert.Single(sequence.Nodes, node => node.Id == "resolved_signal");

        Assert.Equal("resolved_signal", resolved.Next);
        Assert.Contains("停云", relief.Text);
        Assert.Contains("霜", relief.Text);
        Assert.Contains(relief.Events, e => e.Type == "quest_flag"
                                            && e.Key == "senior_brother_mis_resolved"
                                            && e.Value == "true");
    }

    [Fact]
    public void Confrontation_DoesNotTriggerLethalCombat()
    {
        var yaml = ReadAllYaml();

        Assert.DoesNotContain("combat_trigger", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lethal", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("击败师兄", yaml);
        Assert.DoesNotContain("生死战", yaml);
        Assert.DoesNotContain("杀了你", yaml);
        Assert.DoesNotContain("拔剑相向", yaml);
    }

    [Fact]
    public void BurialPrecedesTutorialAndCarriesEmotionalResolution()
    {
        var burialText = ReadDialogueText("joint_burial_01.yaml");
        var inheritanceText = ReadDialogueText("farewell_inheritance_01.yaml");
        var inheritanceEvents = AllEvents(new[] { Load("farewell_inheritance_01.yaml") });
        var burialEvents = AllEvents(new[] { Load("joint_burial_01.yaml") });

        Assert.Contains("一个一个记", burialText);
        Assert.Contains("新坟", burialText);
        Assert.Contains("没有再说血书", burialText);
        Assert.Contains("埋完最后一抔土", inheritanceText);
        Assert.Contains("自保", inheritanceText);
        Assert.Contains("风止尺法", inheritanceText);
        Assert.Contains(inheritanceEvents, e => e.Type == "tutorial_step_unlocked"
                                                && e.Key == "tut_combat_basic");
        Assert.DoesNotContain(burialEvents, e => e.Type == "tutorial_step_unlocked");
    }

    [Fact]
    public void LetterPromise_UnlocksContactWithoutOpeningLetterNode()
    {
        var sequence = Load("letter_promise_01.yaml");
        var text = ReadDialogueText("letter_promise_01.yaml");
        var events = AllEvents(new[] { sequence });

        Assert.Contains("托信", text);
        Assert.Contains("山风未止", text);
        Assert.Contains("空白信笺", text);
        Assert.DoesNotContain(sequence.Nodes, node => node.Type == "letter");
        Assert.Contains(events, e => e.Type == "unlock_letter_contact"
                                     && e.Key == "senior_brother_letter_contact_unlocked"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "tutorial_step_unlocked"
                                     && e.Key == "tut_letter"
                                     && e.Value == "pending");
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

    private static DialogueEventSpec[] AllEvents(IEnumerable<DialogueSequence> sequences)
    {
        return sequences
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => node.Events)
            .ToArray();
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
