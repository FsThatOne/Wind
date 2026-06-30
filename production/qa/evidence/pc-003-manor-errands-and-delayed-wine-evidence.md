# pc-003 Evidence: 山庄寿宴跑腿与拖延取酒

> **Story**: `production/epics/prologue-content/stories/pc-003-manor-errands-and-delayed-wine.md`  
> **Date**: 2026-06-30  
> **Type**: Config/Data  
> **Status**: PASS

## Files

- `feng-zhi/assets/data/dialogues/chapter_00/manor_errands_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/master_study_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/sister_wine_reminder_01.yaml`
- `tests/unit/narrative/prologue_manor_errands_dialogue_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `manor_errands_01.yaml`, `master_study_01.yaml`, and `sister_wine_reminder_01.yaml` include Bai Tan, the manor master, kitchen disciple, pharmacy disciple, and junior brother. |
| AC-2 | `PrologueManorErrandsDialogueTest.WalkThroughAtLeastThreeDailyAreas` checks kitchen, pharmacy, training ground, and main hall references. |
| AC-3 | `sister_wine_reminder_01.yaml` contains two Bai Tan reminders: one early reminder and one dusk warning, with gentle escalation. |
| AC-4 | The delay is expressed through helping others, familiar roads, and the manor's warmth; the test checks there is no countdown, failure, deduction, or game over language. |
| AC-5 | `master_study_01.yaml` shows the seam under the old lamp and records `prologue_study_hidden_compartment_seen`, while the test checks it does not reveal identity, documents, culprit, or Lan kingdom context. |
| AC-6 | `sister_wine_reminder_01.yaml` records `prologue_wine_delayed=true`. |

## Dependency Note

- `pc-001` and `pc-002` have implementation evidence and passing tests, but have not yet been closed via `/story-done`. This story proceeded on that implementation basis.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter PrologueManorErrandsDialogueTest` — PASS, 5/5.
- `dotnet test FengZhi.slnx` — PASS, 1597/1597.
