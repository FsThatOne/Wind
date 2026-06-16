using FengZhi.Foundation.NpcState;

namespace FengZhi.Foundation.Romance;

/// <summary>
/// UI-facing relationship presentation data that intentionally hides romance numbers and internal state keys.
/// </summary>
public sealed record RomanceRelationshipPresentation(
    string DescriptionKey,
    string ToneTag,
    IReadOnlyList<string> MemoryFragmentIds
);

/// <summary>
/// Converts NPC-owned romance state into literary-safe keys for Presentation consumers.
/// </summary>
public static class RelationshipPresentationMapper
{
    private static readonly IReadOnlyList<string> EmptyMemories = Array.Empty<string>();

    /// <summary>
    /// Builds a literary-safe presentation snapshot from attitude and milestone state.
    /// </summary>
    public static RomanceRelationshipPresentation Map(
        AttitudeLevel? attitude,
        RomanceMilestoneState? milestones)
    {
        if (attitude == null || milestones == null)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.unknown",
                "unknown",
                EmptyMemories);
        }

        var memories = BuildMemoryFragmentIds(milestones);
        if (milestones.Broken)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.sundered_path",
                "severed",
                memories);
        }

        if (milestones.Bond)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.shared_vow",
                "vowed",
                memories);
        }

        if (milestones.Heart)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.heart_known",
                "intimate",
                memories);
        }

        if (milestones.Crisis)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.storm_crossed",
                "tested",
                memories);
        }

        if (milestones.Trust)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.trust_placed",
                "trusting",
                memories);
        }

        if (milestones.Acquainted)
        {
            return new RomanceRelationshipPresentation(
                "romance.relationship.description.name_remembered",
                "familiar",
                memories);
        }

        return attitude.Value switch
        {
            AttitudeLevel.DrawnSword => new RomanceRelationshipPresentation(
                "romance.relationship.description.blades_between",
                "hostile",
                EmptyMemories),
            AttitudeLevel.HostileGuard => new RomanceRelationshipPresentation(
                "romance.relationship.description.anger_guarded",
                "guarded",
                EmptyMemories),
            AttitudeLevel.ColdShoulder => new RomanceRelationshipPresentation(
                "romance.relationship.description.cold_distance",
                "distant",
                EmptyMemories),
            AttitudeLevel.Wary => new RomanceRelationshipPresentation(
                "romance.relationship.description.unsettled_silence",
                "wary",
                EmptyMemories),
            AttitudeLevel.Stranger => new RomanceRelationshipPresentation(
                "romance.relationship.description.passing_paths",
                "neutral",
                EmptyMemories),
            AttitudeLevel.Friendly => new RomanceRelationshipPresentation(
                "romance.relationship.description.warm_exchange",
                "warm",
                EmptyMemories),
            AttitudeLevel.Trusted => new RomanceRelationshipPresentation(
                "romance.relationship.description.words_kept",
                "trusting",
                EmptyMemories),
            AttitudeLevel.LifeDeath => new RomanceRelationshipPresentation(
                "romance.relationship.description.life_debt",
                "devoted",
                EmptyMemories),
            _ => new RomanceRelationshipPresentation(
                "romance.relationship.description.unknown",
                "unknown",
                EmptyMemories)
        };
    }

    private static IReadOnlyList<string> BuildMemoryFragmentIds(RomanceMilestoneState milestones)
    {
        var memoryIds = new List<string>(6);
        if (milestones.Acquainted) memoryIds.Add("romance.memory.first_meeting");
        if (milestones.Trust) memoryIds.Add("romance.memory.trust_given");
        if (milestones.Crisis) memoryIds.Add("romance.memory.storm_survived");
        if (milestones.Heart) memoryIds.Add("romance.memory.heart_confession");
        if (milestones.Bond) memoryIds.Add("romance.memory.shared_vow");
        if (milestones.Broken) memoryIds.Add("romance.memory.parting_blade");
        return memoryIds;
    }
}
