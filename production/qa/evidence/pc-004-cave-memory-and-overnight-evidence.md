# pc-004 Evidence: 崖洞回忆互动与夜宿

> **Story**: `production/epics/prologue-content/stories/pc-004-cave-memory-and-overnight.md`  
> **Date**: 2026-06-30  
> **Type**: Config/Data  
> **Status**: PASS

## Files

- `feng-zhi/scripts/BackMountainCliffCaveGame.cs`
- `feng-zhi/assets/data/dialogues/chapter_00/wine_pickup_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/rest_spot_01.yaml`
- `tests/unit/narrative/prologue_cave_memory_content_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `BackMountainCliffCaveGame.RegisterChapter00InsightNodes()` registers four memory-focused nodes: wall technique sketch, small stool memory, wine stain pattern, and medicine pot residue. Existing `wine_pickup_01.yaml`, `rest_spot_01.yaml`, and `memory_marker_01.yaml` provide matching interactable dialogue. |
| AC-2 | `cave_loose_brick` and `ch00_loose_brick` were removed from the active cave node registration and replaced by `cave_wall_technique_sketch`. |
| AC-3 | The four InsightNode narratives and cave dialogue text reference concrete Bai Tan memories: technique sketch, wine sealing, resting nook, snack/stool, and medicine pot. |
| AC-4 | `wine_pickup_01.yaml` now records `prologue_wine_obtained=true` after taking or examining the birthday wine. |
| AC-5 | `rest_spot_01.yaml` night text now explains overnight rest through mountain rain, dark roads, and slippery stone steps; it records `prologue_cave_overnight=true`. |
| AC-6 | `PrologueCaveMemoryContentTest.CaveMemoryContent_DoesNotPolluteWithConspiracyOrRewardSignals` checks the cave content does not contain loose-brick, hidden-compartment, culprit, outsider, code phrase, martial fragment, or loot signals. |

## Dependency Note

- `pc-001` through `pc-003` and `ei-009` have implementation evidence and passing tests, but the prologue-content stories have not yet been closed via `/story-done`. This story proceeded on that implementation basis.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter PrologueCaveMemoryContentTest`
  - PASS: 5 passed, 0 failed, 0 skipped.
- `dotnet test FengZhi.slnx`
  - PASS: 1604 passed, 0 failed, 0 skipped.
