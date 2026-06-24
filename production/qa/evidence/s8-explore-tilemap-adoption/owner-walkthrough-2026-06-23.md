# S8-Explore-TileMap-Adoption · Owner 实机走查 evidence (2026-06-23)

**Story**: [S8-Explore-TileMap-Adoption](../../../sprints/sprint-8-explore-tilemap-adoption.md)
**关联 commit**: `717a23e feat(s8-explore-tilemap): AC1+AC2+AC3 partial — explore TileMapLayer 化骨架`
**关键修正**: `cliff_cave_ground_tiles.tres` `tile_layout: 0 (Stacked) → 5 (DiamondDown)`
**目的**: 在 owner 实机 Play 下确认 TileMapLayer 渲染、Day/Night 切换、玩家行走、深度排序、Boundary collision 是否符合预期；为 AC2 / AC3 关账提供视觉证据。
**Smoke 兜底**: headless run_project 输出 `[BackMountainCliffCave] Ready. Godot TileMapLayer visuals + TMX markers active.`，0 errors（agent 已校验）

---

## 启动方式

1. 在 Godot 编辑器里打开 `feng-zhi/project.godot`
2. F5 / 顶部 Play 按钮启动主场景（`scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn`）
3. 期望进入「后山崖洞」场景，看到 isometric 视角下的洞穴地形

---

## 走查 Checklist

### ✅ Check 1：地块视觉首映（决定 AC2 是否真完成）

**步骤**：场景打开后 5 秒内静止观察整个 cave

**检查项**:
- [ ] **Diamond 视觉**：tile 呈 64×32 的菱形，**不是**矩形 / 平行四边形 / 错位偏移
- [ ] **拼接无缝**：相邻 tile 边缘对齐，无白边、缝隙、重叠
- [ ] **Tileset 内容**：能看到地面、岩壁、墙体 3 层视觉层次（Ground / Terrain / Overlay）
- [ ] **整体布局**：与 [back_mountain_cliff_cave_day.tmx 在 Tiled 中的预览](../../../../feng-zhi/assets/maps/back_mountain_cliff_cave/maps/back_mountain_cliff_cave_day.tmx) 视觉**形状大致对齐**（同样的洞口位置、走道走向）

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图**：保存为 `01-day-overview.png`
**问题/备注**：
```
（写下偏差或意外）
```

---

### ✅ Check 2：Day → Night 切换（决定子 scene visibility 切换是否正确）

**步骤**：按 N 键切换日夜（`CavePlayer` 已实现 N 键）

**检查项**:
- [ ] 按 N 之后场景**整体调色**变化（日 / 夜 tile 不同）
- [ ] 切换**无闪烁、无残影**（Day TileLayers 关 + Night TileLayers 开 应该是瞬时的）
- [ ] 玩家位置 / 道具 / Markers 不变
- [ ] 再按一次 N 能切回 Day

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图**：保存为 `02-night-overview.png`
**问题/备注**：
```

```

---

### ✅ Check 3：玩家 4 斜向走路（验证 ADR-0022 Iso4 + tile_layout 修正后玩家与地块的视觉一致性）

**步骤**：使用 WASD 在主可达区域走 4 个斜方向

**检查项**:
- [ ] **W (NW ↖)**：角色 sprite 切到 walk_nw 动画 + 实际朝屏幕左上走
- [ ] **A (SW ↙)**：sprite 切到 walk_sw + 实际朝屏幕左下走
- [ ] **S (SE ↘)**：sprite 切到 walk_se + 实际朝屏幕右下走
- [ ] **D (NE ↗)**：sprite 切到 walk_ne + 实际朝屏幕右上走
- [ ] **idle 兜底**：松开按键 sprite 切到 idle 帧
- [ ] **方向不抖**：按 WS 之间临界对角时 sprite 不在 NE / NW 之间反复切换（迟滞 ±15° 生效）

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图（每方向各 1）**：`03a-walk-ne.png` / `03b-walk-se.png` / `03c-walk-sw.png` / `03d-walk-nw.png`
**问题/备注**：
```

```

---

### ✅ Check 4：Boundary 不可穿（验证 walkable 判定与 .tmx Ground 数据是否对齐）

**步骤**：尝试走到 cave 边界 / 墙体 / 岩壁

**检查项**:
- [ ] 走到非 Ground 区域（墙体）会**被挡住**（`_groundTiles` HashSet walkable 判定生效）
- [ ] 顶点 / 角落区域不卡死（能正常贴边走）
- [ ] **不**能走出 cave 视觉边界

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图**：`04-boundary-blocked.png`
**问题/备注**：
```

```

---

### ✅ Check 5：y-sort 深度排序（验证 ADR-0022 §4 `y_sort_enabled = true` 在 sub-scene 嵌套场景下生效）

**步骤**：玩家走到 Overlay tile（树冠 / 高墙 / 屋檐）附近，从下方走过和从上方走过

**检查项**:
- [ ] **下方→上方**：玩家 y 比 Overlay tile 大时，Overlay 应**遮挡**玩家
- [ ] **上方→下方**：玩家 y 比 Overlay tile 小时，玩家应**遮挡** Overlay
- [ ] **Day/Night 切换后**遮挡仍正确

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图（前后对比）**：`05a-ysort-behind.png` / `05b-ysort-front.png`
**问题/备注**：
```

```

---

### ✅ Check 6：交互节点（Structures + LogicMarkers，对于本次 commit 是回归保障，不是新功能）

**步骤**：走到 oil_lamp / wine_jar / rest_mat 等道具附近，按 E / 空格 调查

**检查项**:
- [ ] 走到道具附近 PromptLabel 显示 `左键移动 E/空格 调查`
- [ ] 按 E 触发对应文案 / 拾取
- [ ] InventoryLabel 状态文本变化（如取得寿酒）
- [ ] 之前已 PASS 的功能不出现回归

**Verdict**: ☐ PASS  ☐ FAIL  ☐ PARTIAL
**截图（任 1 处交互）**：`06-interact-prompt.png`
**问题/备注**：
```

```

---

## 总结

**整体 Verdict**: ☐ PASS（AC2 + AC3 partial 可关账）  ☐ CONCERNS（个别 check FAIL/PARTIAL，需修复）  ☐ FAIL（AC2 不成立，需重做）

**发现的问题（按严重度排序）**：
1.
2.
3.

**下一步建议**：
- 若 PASS：推进 AC4 props y_sort + AC5 录屏 + AC6/AC7 文档关账
- 若 CONCERNS：fix list 进 task；之后再走查
- 若 FAIL：回滚 commit 717a23e，重新设计 sub-scene 结构

---

## 附录：Godot 控制台输出（如果有 ERROR / WARN 请贴这里）

```
（贴 Godot 编辑器底部 Output 面板的 ERROR / WARN）
```
