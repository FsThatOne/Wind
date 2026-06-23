# Archive: 8-direction sprites (2026-06-22)

Pre-ADR-0022 8-direction walk sprites for `CavePlayer`. Archived as part of
S7-Iso-Pivot-Foundation AC6 (commit ADR-0022 落地 stage 2).

## 内容

`walk/` 含 4 个**卡式方向**（E / N / S / W）的 4 帧序列：
- walk_e_{00..03}.png + .import
- walk_n_{00..03}.png + .import
- walk_s_{00..03}.png + .import
- walk_w_{00..03}.png + .import

> 4 个**斜方向**（NE / SE / SW / NW）已保留在
> `feng-zhi/assets/character/main_character_iso4/walk/` 中继续使用。

## 状态

- ADR-0022 §5：8 向 sprite 规格 **作废**。
- `.gdignore` 同目录文件让 Godot 4.7 编辑器忽略本目录，不会触发 import。
- ADR-0022 §"Migration Plan / Rollback plan"：保留一个 sprint，Sprint 8 retro 时
  若 isometric playtest 失败可一次性回滚（恢复整个 8dir 集合 + main_character.tres 旧版）。

## 不要做

- ❌ 不要从 `main_character.tres` 引用 archive 路径
- ❌ 不要把 archive PNG 添加到任何运行时 scene
- ❌ 不要重新启用 `IDirectionalCharacterAnimator`（已删除）
