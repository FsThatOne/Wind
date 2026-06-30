# pc-002 Evidence: 师姐采药/采矿教学内容

> **Story**: `production/epics/prologue-content/stories/pc-002-sister-gathering-tutorial-content.md`  
> **Date**: 2026-06-30  
> **Type**: Config/Data  
> **Status**: PASS

## Files

- `feng-zhi/assets/data/dialogues/chapter_00/sister_gather_herb_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/sister_gather_ore_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/animal_tracks_mount_foreshadow_01.yaml`
- `tests/unit/narrative/prologue_sister_gathering_dialogue_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `sister_gather_herb_01.yaml` and `sister_gather_ore_01.yaml` explain that the protagonist was protected too well, and that Bai Tan feared falls, cuts, and insects, so she had not formally taught gathering before. |
| AC-2 | Herb and ore each have a separate dialogue sequence with literary action guidance using `按住 E`. |
| AC-3 | `animal_tracks_mount_foreshadow_01.yaml` identifies the tracks as animal tracks from a small deer-like mountain beast. |
| AC-4 | Bai Tan says some animals understand people and can carry riders over long distances; the content only foreshadows mounts and does not open a system tutorial. |
| AC-5 | `PrologueSisterGatheringDialogueTest.TreatTracksAsAnimalsAndForeshadowMountsOnly` checks the text does not include external-conspiracy terms such as 外人、敌人、陌生人、阴谋、欲言又止、凶手. |
| AC-6 | `animal_tracks_mount_foreshadow_01.yaml` records `prologue_gathering_taught=true` and `prologue_mount_foreshadowed=true`; the test asserts both keys. |

## Dependency Note

- `pc-001` has implementation evidence and passing tests, but has not yet been closed via `/story-done`. This story proceeded on that implementation basis.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter PrologueSisterGatheringDialogueTest` — PASS, 5/5.
- `dotnet test FengZhi.slnx` — PASS, 1592/1592.
