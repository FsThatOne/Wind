# Epic: 对话系统

> **Layer**: Core（MVP 早期 / 低依赖入口系统）
> **GDD**: design/gdd/dialogue-system.md
> **Architecture Module**: `Core/Dialogue/`
> **Status**: Complete
> **Stories**: 9/9 Complete (ds-001 ~ ds-009)

## Overview

对话系统是《风止》的叙事骨架管道，负责承载角色对白、剧情分支、心境选择、洞察提示、暗号选项、书信和内心独白。它按照 `Core/Dialogue/` 模块边界实现节点图、条件分支和变量绑定，通过 EventBus 向心境、感情、叙事、NPC 状态、战斗和 UI 等系统发布事件，但不持有下游系统逻辑。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: Dialogue Data Format | 使用 YAML 自研节点图格式，支持 7 种节点类型、条件三元组、事件附加和 CI 校验 | LOW |
| ADR-0003: Data Configuration Format | 静态配置统一使用 YAML 1.2 + YamlDotNet，并由 DataRegistry 加载 | LOW |
| ADR-0001: Event Bus Architecture | 跨层通知使用类型安全 EventBus，场景内通信使用 Godot Signal | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 对话节点图与 7 种节点类型 | ADR-0005 ✅ |
| 条件三元组与可见选项过滤 | ADR-0005 ✅ |
| 对话版本号与 YAML 数据格式 | ADR-0005 ✅ / ADR-0003 ✅ |
| 节点与选项事件附加 | ADR-0005 ✅ / ADR-0001 ✅ |
| 洞察提示、追查与暗号选项 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| 书信、书信匣与内心独白 | ⚠️ GDD 覆盖，需在 stories 中细化验证 |
| UI 展示、打字机效果、选择面板与信笺界面 | ⚠️ UX Flag，需后续 `/ux-design` 输出 |

## Trace Notes

`docs/architecture/tr-registry.yaml` 已登记 `TR-dialogue-system-001` ~ `TR-dialogue-system-009`，分别映射 ds-001 ~ ds-009。每条 story 均引用具体 GDD 条款、Accepted ADR、Control Manifest 版本和对应 TR-ID。

UI 相关 story 以 GDD 的 MVP 视觉/输入规则作为 readiness 基线；后续 `/ux-design` 可继续细化对话框、选择界面、信笺界面和洞察效果，但不阻塞当前 MVP story readiness。

## 完成定义

此 epic 满足以下条件时视为完成：
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/dialogue-system.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 对话 YAML 节点图可加载、遍历、校验，并能检测重复 id、无效 next、孤立节点和 500 节点死循环保护
- 条件分支、洞察提示、暗号选项、书信、内心独白和批量事件分发均有自动化或证据验证
- 对话 UI、选择 UI、信笺 UI 和洞察效果有 UX spec 或 QA evidence 签收

## Stories

| ID | Title | Type | Priority | Depends On | Status |
|----|-------|------|----------|------------|--------|
| ds-001 | 对话 YAML 图节点模型与加载校验 | Config/Data | P0 | — | Complete |
| ds-002 | 对话运行时状态机与节点推进 | Logic | P0 | ds-001 | Complete |
| ds-003 | 条件评估与选项过滤 | Logic | P0 | ds-001, ds-002 | Complete |
| ds-004 | 批量事件队列与 EventBus 分发 | Integration | P0 | ds-001, ds-002 | Complete |
| ds-005 | 洞察门槛、视觉提示与追查分支 | Integration | P1 | ds-002, ds-003, ds-004 | Complete |
| ds-006 | 暗号簿与暗号选项 | Logic | P1 | ds-003 | Complete |
| ds-007 | 书信节点、书信匣与重读规则 | Integration | P1 | ds-002, ds-004, ds-006 | Complete |
| ds-008 | 对话 UI 核心呈现与输入 | UI | P1 | ds-002, ds-007 | Complete |
| ds-009 | 选择 UI、心境暗示与特殊选项呈现 | UI | P1 | ds-003, ds-005, ds-006, ds-008 | Complete |

## 下一步

dialogue-system 已完成 `ds-001` ~ `ds-009`。后续可进入正式 Presentation/UI 场景接入，或切换到 save-system 的 `ss-001`。
