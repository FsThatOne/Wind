# Tech Debt Register

- **2026-06-12** (ma-001 武学 YAML 数据模型): 语义校验错误（id 重复、数值非法等）报告 `lineNumber: null`，仅 YAML parse 错误含行号；可通过自定义 node deserializer 记录每条记录 Mark 增强 — tracked from production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md
- **2026-06-12** (ma-001 武学 YAML 数据模型): 配置数据模型使用可变 `get; set;` setter，未完全落实 ADR-0003 "运行时只读"；CharacterData 层同样如此 — 建议全局统一改为 `init` setter + `AsReadOnly()` 包装（一次性重构，跨 Foundation 层）— tracked from production/epics/martial-arts-system/stories/ma-001-martial-arts-yaml-schema.md
