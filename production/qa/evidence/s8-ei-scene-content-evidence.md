# S8-EI-Scene-Content Evidence

> **Scope**: chapter_00 cave InsightNode content configuration
> **Date**: 2026-06-29
> **Tester**: dev

## Configured Nodes

| Node | Type | Tile | Threshold | Reward |
|---|---|---:|---:|---|
| `cave_loose_brick` | Clue | (25, 5) | 5 | `insight_cave_loose_brick=true` |
| `cave_old_letter_trace` | CodePhrase | (24, 6) | 8 | `cliff_cave_old_letter_mark` |
| `cave_wine_stain_pattern` | EnvironmentDetail | (21, 9) | 5 | none |
| `cave_medicine_pot_residue` | EnvironmentDetail | (17, 12) | 10 | none |

## Verification

- `dotnet test FengZhi.slnx`: 1582/1582 pass
- `dotnet build feng-zhi/FengZhi.csproj`: 0 warning / 0 error
- `godot --headless --path feng-zhi --import`: PASS

## Notes

- The medicine pot node is implemented as `EnvironmentDetail` for this slice. Full `Loot` dispatch is outside ei-008 / S8-EI-Scene-Content support and remains future work.
- Nodes are registered in `BackMountainCliffCaveGame.RegisterChapter00InsightNodes()` and activated under scene id `back_mountain_cliff_cave`.
