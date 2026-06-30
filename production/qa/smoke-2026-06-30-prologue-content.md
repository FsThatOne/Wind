# Smoke Check Report: Prologue Content Vertical Pass

**Date**: 2026-06-30  
**Sprint**: Sprint 9 / Prologue Content Nice-to-Have Expansion  
**Engine**: Godot 4.7-stable Mono (C# / .NET 8+)  
**QA Plan**: `production/qa/qa-plan-sprint-8-2026-06-29.md` used as nearest available QA plan; prologue content has dedicated story evidence under `production/qa/evidence/pc-00*.md`.  
**Argument**: sprint-style smoke, prologue content scope  

---

## Environment

- Test directory: found (`tests/`)
- CI workflow: found (`.github/workflows/tests.yml`)
- Smoke checklist: found (`tests/smoke/critical-paths.md`)
- Target platform: PC (Steam v1.0), keyboard/mouse and gamepad

## Automated Tests

| Command | Status | Result |
|---|---|---|
| `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "FullyQualifiedName~Chapter00MainlineGraphTest\|FullyQualifiedName~PrologueSisterGatheringDialogueTest\|FullyQualifiedName~PrologueManorErrandsDialogueTest\|FullyQualifiedName~PrologueCaveMemoryContentTest\|FullyQualifiedName~PrologueMassacreEvidenceDialogueTest\|FullyQualifiedName~PrologueSeniorBrotherFarewellContentTest"` | PASS | 31 passed, 0 failed, 0 skipped |
| `dotnet test FengZhi.slnx` | PASS | 1684 passed, 0 failed, 0 skipped |
| `godot --headless --script tests/gdunit4_runner.gd` | NOT RUN / WARNING | Process exited with code 137 in this environment; requires local Godot/headless re-run or CI confirmation |

## Test Coverage

| Story | Type | Test / Evidence | Coverage Status |
|---|---|---|---|
| pc-001: chapter_00 主线节点骨架与状态 key | Config/Data | `tests/unit/narrative/chapter_00_mainline_graph_test.cs`; `production/qa/evidence/pc-001-chapter-00-mainline-graph-evidence.md` | COVERED |
| pc-002: 师姐采药/采矿教学内容 | Config/Data | `tests/unit/narrative/prologue_sister_gathering_dialogue_test.cs`; `production/qa/evidence/pc-002-sister-gathering-tutorial-content-evidence.md` | COVERED |
| pc-003: 山庄寿宴跑腿与拖延取酒 | Config/Data | `tests/unit/narrative/prologue_manor_errands_dialogue_test.cs`; `production/qa/evidence/pc-003-manor-errands-and-delayed-wine-evidence.md` | COVERED |
| pc-004: 崖洞回忆互动与夜宿 | Config/Data | `tests/unit/narrative/prologue_cave_memory_content_test.cs`; `production/qa/evidence/pc-004-cave-memory-and-overnight-evidence.md` | COVERED |
| pc-005: 灭门后回庄搜证路径 | Config/Data | `tests/unit/narrative/prologue_massacre_evidence_dialogue_test.cs`; `production/qa/evidence/pc-005-massacre-evidence-path-evidence.md` | COVERED |
| pc-006: 师兄误会、共同埋葬与临别传承 | Integration | `tests/unit/narrative/prologue_senior_brother_farewell_content_test.cs`; `production/qa/evidence/pc-006-senior-brother-misunderstanding-and-farewell-evidence.md` | COVERED WITH DEPENDENCY NOTES |

**Summary**: 6 covered, 0 missing.  

## Prologue Critical Path

| Path Segment | Automated Evidence | Status |
|---|---|---|
| P00-01 → P00-10 mainline order and state keys | `Chapter00MainlineGraphTest` | PASS |
| Sister gathering and mount foreshadowing | `PrologueSisterGatheringDialogueTest` | PASS |
| Manor errands, master study, delayed wine | `PrologueManorErrandsDialogueTest` | PASS |
| Cave memory nodes, wine obtained, overnight reason | `PrologueCaveMemoryContentTest` | PASS |
| Silent return and massacre investigation | `PrologueMassacreEvidenceDialogueTest` | PASS |
| Senior brother misunderstanding, burial, inheritance, letter promise | `PrologueSeniorBrotherFarewellContentTest` | PASS |

## Manual Smoke Checks

These items were not executed in this shell-only smoke pass and must be confirmed in a Godot build or editor session:

- [ ] Game launches to main menu without crash.
- [ ] New game / session can reach the prologue route.
- [ ] Keyboard/mouse and gamepad can advance dialogue and choices.
- [ ] Player can enter the cave scene and trigger the updated memory InsightNodes.
- [ ] Player can progress from silent return to massacre evidence content.
- [ ] Senior brother sequence can be played in order without missing scene hooks.
- [ ] No major frame hitch or text clipping observed during prologue content.
- [ ] Steam Deck / small-screen Chinese text remains readable.

## Dependency Notes

- `pc-006` intentionally uses dialogue wording and state events as the current substitute for full Misunderstanding FSM/UI integration.
- Real combat tutorial config is not part of this content pass; `farewell_inheritance_01.yaml` records pending tutorial step keys for later hookup.
- Long-term letter content is not part of this content pass; `letter_promise_01.yaml` unlocks / records the senior brother contact.

## Verdict

**PASS WITH WARNINGS**

Automated prologue content and full Foundation tests passed. The build is ready for manual QA / editor walkthrough of the prologue content path, but the Godot headless runner did not complete in this environment and hands-on launch/input/performance checks remain pending.
