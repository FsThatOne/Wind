# pc-001 Evidence: chapter_00 主线节点骨架与状态 key

> **Story**: `production/epics/prologue-content/stories/pc-001-chapter-00-mainline-graph.md`  
> **Date**: 2026-06-30  
> **Type**: Config/Data  
> **Status**: PASS

## Files

- `assets/data/narrative/chapter_00.yaml`
- `tests/unit/narrative/chapter_00_mainline_graph_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `chapter_00.yaml` defines 10 unique P00 nodes. Each node has `id`, `title`, `type`, `preconditions`, `completion_condition`, and `on_complete`. |
| AC-2 | `Chapter00MainlineGraphTest.Chapter00Yaml_ChainsNodesThroughApprovedOneDayOneNightStructure` checks the approved sequence from waking in the manor through senior brother farewell. |
| AC-3 | `Chapter00MainlineGraphTest.Chapter00Yaml_DefinesRequiredStoryStateKeys` checks all required state keys are present in node completion events. |
| AC-4 | `Chapter00MainlineGraphTest.Chapter00Yaml_ExcludesCombatAndConspiracySignalsBeforeFarewell` checks pre-farewell nodes contain no combat node and no forbidden early-conspiracy phrases. |
| AC-5 | `Chapter00MainlineGraphTest.Chapter00Yaml_MarksSeniorBrotherDependenciesWithoutBlockingEarlierNodes` checks P00-10 is a dialogue node that marks misunderstanding and combat tutorial dependencies. |

## Notes

- `content_dependencies` and `completion_condition` are content-authoring fields. The current `NarrativeGraphLoader` ignores unmatched YAML properties, so the graph remains loadable while preserving authoring intent for later content tools.
- `pc-001` does not implement full dialogue text, the misunderstanding FSM, or combat tutorial. Those remain scoped to `pc-002` through `pc-006`.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter Chapter00MainlineGraphTest` — PASS, 5/5.
- `dotnet test FengZhi.slnx` — PASS, 1587/1587.
