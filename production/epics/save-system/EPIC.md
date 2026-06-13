# Epic: 存档系统

> **Layer**: Platform
> **GDD**: design/gdd/save-system.md
> **Architecture Module**: `Platform/Save/`
> **Status**: Done
> **Stories**: 6 stories (ss-001 ~ ss-006)

## Overview

存档系统是《风止》叙事承诺的技术载体，负责把世界状态、角色状态、对话历史、心境、队伍、物品和系统元数据序列化到本地磁盘，并在加载时精确恢复。它按照 `Platform/Save/` 模块边界实现 SaveManager、槽位管理、加密、完整性校验、版本迁移、异步写入和 `ISaveable` 注册分发，不持有具体游戏逻辑。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0004: Save Encryption & Persistence Strategy | 使用 JSON payload、AES-256-CBC、HMAC、版本迁移链、10 手动槽 + 1 自动槽和 DEBUG 明文旁路 | LOW |
| ADR-0001: Event Bus Architecture | 存档完成、加载完成、失败等跨层通知走类型安全 EventBus，存档订阅使用最低 priority | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| AES-256 对称密钥加密 | ADR-0004 ✅ |
| HMAC 完整性校验与篡改拒绝 | ADR-0004 ✅ |
| 开发构建明文 `.dev.json` dump | ADR-0004 ✅ |
| 版本迁移链与 schema_version | ADR-0004 ✅ |
| 10 个手动槽 + 1 个自动槽 | ADR-0004 ✅ |
| 存档元数据、缩略图、章节、日期和游戏时长 | ADR-0004 ✅ |
| 自动存档触发、延迟和禁止条件 | ADR-0004 ✅ |
| 异步写入，不阻塞主线程 | ADR-0004 ✅ |
| 各系统通过 `ISaveable` 注册序列化器 | ADR-0004 ✅ |
| 存档/读档菜单与槽位 UI | Presenter/DTO 契约已完成；正式 Godot Control 视觉验证留到 Presentation 接入 |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前暂无真实 TR 条目，因此本 epic 先以 GDD Acceptance Criteria、ADR-0004 和 ADR-0001 作为追踪来源。Godot 4.4+ `FileAccess.store_*` 返回值与 C# 绑定行为需要在实现 story 中单独验证。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/save-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 加密、解密、HMAC 篡改拒绝、迁移链、槽位上限、自动存档阻断和磁盘写入失败回滚均有测试覆盖
- DEBUG 构建写出 `.dev.json`，正式构建不写明文旁路
- 存档/读档菜单、槽位元数据、覆盖确认、删除确认和阻断状态有 UX spec 或 QA evidence 签收

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| ss-001 | 存档 payload 与槽位元数据模型 | Config/Data | P0 | — | Complete |
| ss-002 | SaveManager 核心读写与 ISaveable 分发 | Integration | P0 | ss-001 | Complete |
| ss-003 | 存档加密、HMAC 与文件头校验 | Logic | P0 | ss-001 | Complete |
| ss-004 | 版本迁移链与明文调试旁路 | Logic | P1 | ss-001, ss-003 | Complete |
| ss-005 | 自动存档触发、阻断条件与排队 | Integration | P1 | ss-002, ss-004 | Complete |
| ss-006 | 存档/读档菜单与槽位交互 | UI | P1 | ss-001, ss-002, ss-005 | Complete |

## 下一步

save-system 已完成 `ss-001` ~ `ss-006`。下一步建议运行 `/story-done` 做整组收口，或进入 Presentation/UI 层接入正式 Godot Control 存档/读档菜单。
