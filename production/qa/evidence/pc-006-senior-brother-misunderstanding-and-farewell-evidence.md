# pc-006 Evidence: 师兄误会、共同埋葬与临别传承

> **Story**: `production/epics/prologue-content/stories/pc-006-senior-brother-misunderstanding-and-farewell.md`  
> **Date**: 2026-06-30  
> **Type**: Integration  
> **Status**: PASS WITH DEPENDENCY NOTES

## Files

- `feng-zhi/assets/data/dialogues/chapter_00/senior_brother_return_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/senior_brother_misunderstanding_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/joint_burial_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/farewell_inheritance_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/letter_promise_01.yaml`
- `tests/unit/narrative/prologue_senior_brother_farewell_content_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `senior_brother_return_01.yaml` registers `mis_senior_brother_survivor_suspicion` from the senior brother seeing only the protagonist alive with the blood letter and wine. |
| AC-2 | `PrologueSeniorBrotherFarewellContentTest.MisunderstandingSource_StaysOnOnlySurvivorNotIdentity` blocks identity terms including 澜国, 旧玉, 身世, and 异族. |
| AC-3 | The confrontation content contains no `combat_trigger`, lethal combat marker, beat-senior-brother objective, or life-and-death duel text. |
| AC-4 | `senior_brother_misunderstanding_01.yaml` clarifies wine retrieval, cave overnight, the blood letter, and Bai Tan missing from the bodies. |
| AC-5 | `joint_burial_01.yaml` gives the burial a dedicated sequence before information pursuit resumes. |
| AC-6 | `farewell_inheritance_01.yaml` starts after burial and records tutorial entry points `tut_combat_basic`, `tut_combat_qi_counter`, and `tut_combat_decisive` as pending. |
| AC-7 | `letter_promise_01.yaml` naturally introduces letters and records `senior_brother_letter_contact_unlocked=true` without opening a letter UI node. |
| AC-8 | This evidence records dependency notes for later Misunderstanding FSM/UI integration and real combat tutorial config hookup. |

## Dependency Notes

- Misunderstanding FSM/UI is not implemented in this story. Current substitute: dialogue wording plus `register_misunderstanding` / `senior_brother_mis_resolved` state events.
- Real combat tutorial config is not implemented in this story. Current substitute: narrative tutorial entry plus pending tutorial step keys for `tut_combat_basic`, `tut_combat_qi_counter`, and `tut_combat_decisive`.
- Long-term letter content is not implemented in this story. Current substitute: `senior_brother_letter_contact_unlocked` and `tut_letter` pending state.
- `pc-001` through `pc-005` have implementation evidence and passing tests, but the prologue-content stories have not yet been closed via `/story-done`. This story proceeded on that implementation basis.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter PrologueSeniorBrotherFarewellContentTest`
  - PASS: 6 passed, 0 failed, 0 skipped.
- `dotnet test FengZhi.slnx`
  - PASS: 1615 passed, 0 failed, 0 skipped.
