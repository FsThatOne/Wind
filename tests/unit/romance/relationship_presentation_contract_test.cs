using System.Reflection;
using FengZhi.Foundation.NpcState;
using FengZhi.Foundation.Romance;
using Xunit;

namespace FengZhi.Tests.Foundation.Romance;

public sealed class RelationshipPresentationContractTest
{
    private static readonly string[] ForbiddenContractTerms =
    {
        "affection",
        "progress",
        "percentage",
        "score",
        "attitude",
        "M_TRUST",
        "M_CRISIS",
        "M_HEART",
        "M_BOND",
        "M_BREAK",
        "romance_milestone",
        "bonded_heroine"
    };

    [Theory]
    [InlineData(AttitudeLevel.DrawnSword, "romance.relationship.description.blades_between", "hostile")]
    [InlineData(AttitudeLevel.HostileGuard, "romance.relationship.description.anger_guarded", "guarded")]
    [InlineData(AttitudeLevel.ColdShoulder, "romance.relationship.description.cold_distance", "distant")]
    [InlineData(AttitudeLevel.Wary, "romance.relationship.description.unsettled_silence", "wary")]
    [InlineData(AttitudeLevel.Stranger, "romance.relationship.description.passing_paths", "neutral")]
    [InlineData(AttitudeLevel.Friendly, "romance.relationship.description.warm_exchange", "warm")]
    [InlineData(AttitudeLevel.Trusted, "romance.relationship.description.words_kept", "trusting")]
    [InlineData(AttitudeLevel.LifeDeath, "romance.relationship.description.life_debt", "devoted")]
    public void GetRelationshipPresentation_MapsEveryAttitudeTierToLiteraryKey(
        AttitudeLevel attitude,
        string expectedDescriptionKey,
        string expectedToneTag)
    {
        var service = CreateService(attitude, new RomanceMilestoneState());

        var presentation = service.GetRelationshipPresentation("heroine_a");

        Assert.Equal(expectedDescriptionKey, presentation.DescriptionKey);
        Assert.Equal(expectedToneTag, presentation.ToneTag);
        Assert.Empty(presentation.MemoryFragmentIds);
    }

    [Theory]
    [MemberData(nameof(MilestonePresentations))]
    public void GetRelationshipPresentation_MapsMilestoneCombinationsToLiteraryKeysAndMemories(
        RomanceMilestoneState milestones,
        string expectedDescriptionKey,
        string expectedToneTag,
        string[] expectedMemoryIds)
    {
        var service = CreateService(AttitudeLevel.LifeDeath, milestones);

        var presentation = service.GetRelationshipPresentation("heroine_a");

        Assert.Equal(expectedDescriptionKey, presentation.DescriptionKey);
        Assert.Equal(expectedToneTag, presentation.ToneTag);
        Assert.Equal(expectedMemoryIds, presentation.MemoryFragmentIds);
    }

    [Fact]
    public void GetRelationshipPresentation_DoesNotExposeNumericOrInternalStateContract()
    {
        var service = CreateService(
            AttitudeLevel.LifeDeath,
            new RomanceMilestoneState(
                Acquainted: true,
                Trust: true,
                Crisis: true,
                Heart: true,
                Bond: true));

        var presentation = service.GetRelationshipPresentation("heroine_a");
        var propertyNames = typeof(RomanceRelationshipPresentation)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name);

        foreach (var propertyName in propertyNames)
        {
            AssertNoForbiddenTerm(propertyName);
        }

        AssertNoForbiddenTerm(presentation.DescriptionKey);
        AssertNoForbiddenTerm(presentation.ToneTag);
        Assert.All(presentation.MemoryFragmentIds, memoryId =>
        {
            Assert.StartsWith("romance.memory.", memoryId);
            AssertNoForbiddenTerm(memoryId);
        });
    }

    [Fact]
    public void GetRelationshipPresentation_WhenNpcMissing_ReturnsSafeFallback()
    {
        var service = CreateService(AttitudeLevel.Stranger, new RomanceMilestoneState());

        var presentation = service.GetRelationshipPresentation("missing");

        Assert.Equal("romance.relationship.description.unknown", presentation.DescriptionKey);
        Assert.Equal("unknown", presentation.ToneTag);
        Assert.Empty(presentation.MemoryFragmentIds);
    }

    public static IEnumerable<object[]> MilestonePresentations()
    {
        yield return new object[]
        {
            new RomanceMilestoneState(Acquainted: true),
            "romance.relationship.description.name_remembered",
            "familiar",
            new[] { "romance.memory.first_meeting" }
        };
        yield return new object[]
        {
            new RomanceMilestoneState(Acquainted: true, Trust: true),
            "romance.relationship.description.trust_placed",
            "trusting",
            new[] { "romance.memory.first_meeting", "romance.memory.trust_given" }
        };
        yield return new object[]
        {
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true),
            "romance.relationship.description.storm_crossed",
            "tested",
            new[] { "romance.memory.first_meeting", "romance.memory.trust_given", "romance.memory.storm_survived" }
        };
        yield return new object[]
        {
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true),
            "romance.relationship.description.heart_known",
            "intimate",
            new[]
            {
                "romance.memory.first_meeting",
                "romance.memory.trust_given",
                "romance.memory.storm_survived",
                "romance.memory.heart_confession"
            }
        };
        yield return new object[]
        {
            new RomanceMilestoneState(Acquainted: true, Trust: true, Crisis: true, Heart: true, Bond: true),
            "romance.relationship.description.shared_vow",
            "vowed",
            new[]
            {
                "romance.memory.first_meeting",
                "romance.memory.trust_given",
                "romance.memory.storm_survived",
                "romance.memory.heart_confession",
                "romance.memory.shared_vow"
            }
        };
        yield return new object[]
        {
            new RomanceMilestoneState(Broken: true, Acquainted: true, Trust: true, Crisis: true, Heart: true, Bond: true),
            "romance.relationship.description.sundered_path",
            "severed",
            new[]
            {
                "romance.memory.first_meeting",
                "romance.memory.trust_given",
                "romance.memory.storm_survived",
                "romance.memory.heart_confession",
                "romance.memory.shared_vow",
                "romance.memory.parting_blade"
            }
        };
    }

    private static RomanceService CreateService(AttitudeLevel attitude, RomanceMilestoneState milestones)
    {
        var npcState = new RecordingRomanceNpcStatePort();
        npcState.Register("heroine_a", attitude, milestones);
        return new RomanceService(npcState, new MilestoneRegistry(npcState));
    }

    private static void AssertNoForbiddenTerm(string value)
    {
        Assert.All(ForbiddenContractTerms, forbidden =>
            Assert.DoesNotContain(forbidden, value, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingRomanceNpcStatePort : IRomanceNpcStatePort
    {
        private readonly Dictionary<string, AttitudeLevel> _attitudes = new();
        private readonly Dictionary<string, RomanceMilestoneState> _milestones = new();

        public void Register(string npcId, AttitudeLevel attitude, RomanceMilestoneState milestones)
        {
            _attitudes[npcId] = attitude;
            _milestones[npcId] = milestones;
        }

        public AttitudeLevel? GetAttitude(string npcId)
        {
            return _attitudes.TryGetValue(npcId, out var attitude) ? attitude : null;
        }

        public RomanceMilestoneState? GetMilestones(string npcId)
        {
            return _milestones.TryGetValue(npcId, out var milestones) ? milestones : null;
        }

        public bool SetAttitude(string npcId, AttitudeLevel attitude, string source)
        {
            return false;
        }

        public bool SetMilestone(string npcId, RomanceMilestone milestone, bool value, string source)
        {
            return false;
        }

        public bool ForceBreak(string npcId, AttitudeLevel terminalAttitude, string source)
        {
            return false;
        }

        public string? GetBondedHeroine()
        {
            return null;
        }

        public bool ConfirmBond(string npcId, string source)
        {
            return false;
        }

        public bool HasRomanceFlag(string npcId, string key)
        {
            return false;
        }

        public bool SetRomanceFlag(string npcId, string key, string value, string source)
        {
            return false;
        }
    }
}
