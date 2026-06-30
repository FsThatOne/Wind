# pc-005 Evidence: 灭门后回庄搜证路径

> **Story**: `production/epics/prologue-content/stories/pc-005-massacre-evidence-path.md`  
> **Date**: 2026-06-30  
> **Type**: Config/Data  
> **Status**: PASS

## Files

- `feng-zhi/assets/data/dialogues/chapter_00/massacre_return_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/massacre_evidence_01.yaml`
- `tests/unit/narrative/prologue_massacre_evidence_dialogue_test.cs`

## Acceptance Criteria Evidence

| AC | Evidence |
|---|---|
| AC-1 | `massacre_return_01.yaml` includes silent return beats: no insects, still wind, open manor gate, and missing morning sounds. |
| AC-2 | `massacre_evidence_01.yaml` exposes at least five active investigation options before completion. |
| AC-3 | `blood_letter_01` contains the exact blood-letter text: “风起渊底，鹤归无枝。” |
| AC-4 | `sister_missing_01` and `sister_missing_03` state that Bai Tan is not among the bodies and that the protagonist does not know where she went. |
| AC-5 | `hidden_compartment_01` states the study hidden compartment is empty, without adding direct culprit tokens or faction identifiers. |
| AC-6 | `PrologueMassacreEvidenceDialogueTest.MassacreEvidenceDialogue_DoesNotTriggerCombatOrRevealCulprit` checks that the content contains no combat trigger, encounter, pursuit battle, beatable enemy, or direct culprit reveal. |
| AC-7 | Dialogue events record `prologue_massacre_discovered`, `prologue_blood_letter_obtained`, and `prologue_sister_missing_known`. |

## Dependency Note

- `pc-001` through `pc-004` have implementation evidence and passing tests, but the prologue-content stories have not yet been closed via `/story-done`. This story proceeded on that implementation basis.

## Verification

- `dotnet test tests/Foundation/Foundation.Tests.csproj --filter PrologueMassacreEvidenceDialogueTest`
  - PASS: 5 passed, 0 failed, 0 skipped.
- `dotnet test FengZhi.slnx`
  - PASS: 1609 passed, 0 failed, 0 skipped.
