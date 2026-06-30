using FengZhi.Foundation.Dialogue;
using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueSisterGatheringDialogueTest
{
    private static readonly string[] DialogueFiles =
    {
        "sister_gather_herb_01.yaml",
        "sister_gather_ore_01.yaml",
        "animal_tracks_mount_foreshadow_01.yaml"
    };

    [Fact]
    public void SisterGatheringDialogues_LoadAndProvideHerbOreAndTrackNodes()
    {
        var sequences = LoadAll();

        Assert.Contains(sequences, sequence => sequence.Id == "sister_gather_herb_01");
        Assert.Contains(sequences, sequence => sequence.Id == "sister_gather_ore_01");
        Assert.Contains(sequences, sequence => sequence.Id == "animal_tracks_mount_foreshadow_01");
        Assert.All(sequences, sequence => Assert.NotEmpty(sequence.Nodes));
    }

    [Fact]
    public void SisterGatheringDialogues_ExplainProtectionReasonForLateGatheringLessons()
    {
        var text = ReadAllDialogueText();

        Assert.Contains("被大家护得太好了", text);
        Assert.Contains("怕你摔着、割着、被蛇虫惊着", text);
        Assert.Contains("一直没教", text);
        Assert.Contains("拗不过你", text);
    }

    [Fact]
    public void SisterGatheringDialogues_IncludeCompletableHerbAndOreGuidance()
    {
        var text = ReadAllDialogueText();

        Assert.Contains("按住 E 慢慢采", text);
        Assert.Contains("按住 E，别把整块石头惊裂", text);
        Assert.Contains("prologue_herb_tutorial_seen", ReadAllYaml());
        Assert.Contains("prologue_ore_tutorial_seen", ReadAllYaml());
    }

    [Fact]
    public void SisterGatheringDialogues_TreatTracksAsAnimalsAndForeshadowMountsOnly()
    {
        var text = ReadAllDialogueText();
        var yaml = ReadAllYaml();

        Assert.Contains("山兽", text);
        Assert.Contains("小鹿", text);
        Assert.Contains("能驮人走很远的路", text);
        Assert.Contains("坐骑", text);
        Assert.Contains("prologue_mount_foreshadowed", yaml);

        Assert.DoesNotContain("外人", text);
        Assert.DoesNotContain("敌人", text);
        Assert.DoesNotContain("陌生人", text);
        Assert.DoesNotContain("阴谋", text);
        Assert.DoesNotContain("欲言又止", text);
        Assert.DoesNotContain("凶手", text);
        Assert.DoesNotContain("坐骑系统", text);
    }

    [Fact]
    public void SisterGatheringDialogues_RecordRequiredPrologueStateKeys()
    {
        var sequences = LoadAll();
        var events = sequences
            .SelectMany(sequence => sequence.Nodes)
            .SelectMany(node => node.Events)
            .ToArray();

        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_gathering_taught"
                                     && e.Value == "true");
        Assert.Contains(events, e => e.Type == "quest_flag"
                                     && e.Key == "prologue_mount_foreshadowed"
                                     && e.Value == "true");
    }

    private static DialogueSequence[] LoadAll()
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
        return string.Join("\n", LoadAll()
            .SelectMany(sequence => sequence.Nodes)
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
