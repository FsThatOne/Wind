# rs-007 文学化关系展示契约 Evidence

> **Story**: `production/epics/romance-system/stories/rs-007-literary-relationship-presentation-contract.md`
> **Date**: 2026-06-14
> **Scope**: Feature-layer relationship presentation contract. Full character encyclopedia UI scene is out of scope for rs-007.

## Evidence Summary

`RomanceService.GetRelationshipPresentation(npcId)` returns a UI-facing contract that contains only literary-safe fields:

- `DescriptionKey`
- `ToneTag`
- `MemoryFragmentIds`

The contract does not expose raw affection numbers, progress values, percentages, internal milestone enum names, NPC State flag names, or direct global bond flags.

## Automated Checks

Command:

```bash
dotnet test --filter RelationshipPresentationContractTest
```

Result: 16/16 passing.

Covered scenarios:

- Every `AttitudeLevel` tier maps to a non-empty literary description key and tone tag.
- Milestone combinations map to literary description keys and narrative memory fragment ids.
- Missing NPC state returns a safe fallback key.
- Public DTO property names and sample values do not contain forbidden numeric/progress/internal-state terms.

## Acceptance Mapping

| Acceptance Criterion | Evidence | Status |
| --- | --- | --- |
| AC8: 人物图鉴正确显示文学化关系描述 | `RelationshipPresentationContractTest.GetRelationshipPresentation_MapsEveryAttitudeTierToLiteraryKey` and milestone mapping test | [x] Approved |
| 输出契约不得包含好感度数字、进度条、攻略进度百分比或结缘状态直白提示 | `RelationshipPresentationContractTest.GetRelationshipPresentation_DoesNotExposeNumericOrInternalStateContract` | [x] Approved |
| 各态度档位和里程碑组合必须能映射为文学化描述 key | attitude-tier and milestone-combination tests | [x] Approved |
| 已达成里程碑可作为回忆片段查询，不暴露内部 flag 名称 | milestone memory id assertions and forbidden-term checks | [x] Approved |

## Notes

- This evidence validates the Feature-layer query contract consumed by Presentation.
- Godot Control scene creation, `BaseUiPanel` inheritance, focus behavior, and final character encyclopedia walkthrough remain out of scope for rs-007.
