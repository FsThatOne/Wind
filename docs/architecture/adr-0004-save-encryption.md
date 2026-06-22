# ADR-0004: Save Encryption & Persistence Strategy

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Platform / Persistence |
| **Knowledge Risk** | LOW — 使用 .NET 标准加密库，不依赖 Godot-specific API |
| **References Consulted** | `docs/engine-reference/godot/breaking-changes.md` (FileAccess.store_* return bool change in 4.4) |
| **Post-Cutoff APIs Used** | `FileAccess.store_*` 返回 bool (4.4+，需处理返回值) |
| **Verification Required** | 验证 FileAccess.StoreBuffer() 在 C# 绑定中的返回类型 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (数据格式 — 存档内部使用 JSON 序列化) |
| **Enables** | 所有实现 ISaveable 的系统 |
| **Blocks** | Sprint 1 Save 模块实现 |
| **Ordering Note** | 在存档系统编码开始前必须 Accepted |

## Context

### Problem Statement

《风止》需要一个存档系统来持久化大量叙事状态（心境、关系、暗号、误会、书信 = 复杂嵌套数据）。存档需要防止简单篡改（防止玩家直接编辑 JSON 破坏游戏平衡），同时支持版本迁移（随游戏更新添加新字段）和开发期调试（明文查看存档内容）。

### Constraints

- 单机游戏，非网络验证 — 防篡改目标是"增加难度"而非"绝对安全"
- 密钥内置可执行文件（非服务器存储）— 决定性逆向可破解，接受此限制
- 预估单个存档 100-500KB（JSON 压缩前）
- 存档操作不可阻塞主线程（RPG 自动存档频繁）
- 开发构建需要明文 dump 供调试
- 10 手动槽 + 1 自动槽
- 版本迁移必须链式（v1→v2→v3...），不跳版本

### Requirements

- AES-256 对称加密
- JSON 序列化（与 ADR-0003 一致，存档数据人类可理解）
- 后台线程写入（不阻塞 UI）
- 启动加载 < 500ms（含解密+反序列化+迁移）
- 版本迁移链支持任意旧版本→当前版本
- 开发构建自动 dump 明文 .json
- 存档损坏检测（校验和）

## Decision

采用 **.NET 8 `System.Security.Cryptography` AES-256-CBC** + **System.Text.Json 序列化** + **后台 Task 写入**。

### 加密方案

| 参数 | 值 |
|------|---|
| 算法 | AES-256-CBC |
| 密钥长度 | 256 bit |
| IV | 每次加密随机生成 16 bytes，存储在密文头部 |
| 密钥存储 | 编译时内嵌（`const byte[]`），正式构建 obfuscation 可选 |
| Padding | PKCS7 |
| 校验 | HMAC-SHA256 附加在密文尾部（先加密后签名） |

### 存档文件结构

```
┌─────────────────────────────────────────────┐
│ Header (32 bytes, 明文)                      │
│  - magic: "FZHI" (4 bytes)                  │
│  - schema_version: uint32 (4 bytes)         │
│  - flags: uint32 (4 bytes)                  │
│  - reserved: 20 bytes                       │
├─────────────────────────────────────────────┤
│ IV (16 bytes, 明文)                          │
├─────────────────────────────────────────────┤
│ Encrypted Payload (variable)                │
│  - JSON UTF-8 bytes → AES-256-CBC           │
├─────────────────────────────────────────────┤
│ HMAC-SHA256 (32 bytes)                      │
│  - HMAC(header + IV + payload)              │
└─────────────────────────────────────────────┘
```

### Architecture Diagram

```
SaveGame() 调用流程:

  主线程                              后台线程
  ─────────                          ──────────
  1. 收集各系统 ISaveable.Serialize()
  2. 组装 SavePayload (Dictionary)
  3. 创建 Task →                      4. JSON 序列化
                                      5. 生成随机 IV
                                      6. AES-256-CBC 加密
                                      7. 计算 HMAC-SHA256
                                      8. 写入文件头 + IV + 密文 + HMAC
                                      9. (DEV) 写明文 .dev.json
                                      10. 返回 Result
  11. ← await Task
  12. 发布 SaveCompletedEvent
```

```
LoadGame() 调用流程:

  主线程
  ─────────
  1. 读取文件全部字节
  2. 验证 magic bytes ("FZHI")
  3. 提取 schema_version
  4. 验证 HMAC-SHA256 （完整性检查）
  5. 提取 IV + 密文
  6. AES-256-CBC 解密 → JSON bytes
  7. 版本迁移链: while (saved_ver < current_ver) Migrate()
  8. JSON 反序列化 → Dictionary
  9. 分发到各系统 ISaveable.Deserialize()
  10. 发布 SaveLoadedEvent
```

### Key Interfaces

```csharp
// Platform/Save/ISaveManager.cs
public interface ISaveManager
{
    Task<SaveResult> SaveGame(int slot);
    SaveResult LoadGame(int slot);
    IReadOnlyList<SlotMetadata> ListSlots();
    void DeleteSlot(int slot);
    SlotMetadata GetMetadata(int slot);
    bool IsSlotOccupied(int slot);
}

// Platform/Save/SaveResult.cs
public record SaveResult(bool Success, string Error = null);

// Platform/Save/SlotMetadata.cs
public record SlotMetadata(
    int Slot,
    DateTime Timestamp,
    string ChapterName,
    string SceneName,
    int GameDay,
    int PlaytimeSeconds,
    bool IsNewGamePlus,
    byte[] Thumbnail  // 128x72 PNG
);

// Platform/Save/ISaveable.cs (Shared contract)
public interface ISaveable
{
    string SaveKey { get; }
    Dictionary<string, object> Serialize();
    void Deserialize(Dictionary<string, object> data, int version);
}

// Platform/Save/IMigration.cs
public interface IMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    Dictionary<string, object> Migrate(Dictionary<string, object> data);
}
```

### 版本迁移链

```csharp
// Platform/Save/MigrationChain.cs
public class MigrationChain
{
    private readonly SortedList<int, IMigration> _migrations = new();

    public void Register(IMigration migration)
    {
        _migrations.Add(migration.FromVersion, migration);
    }

    public Dictionary<string, object> Apply(Dictionary<string, object> data, int fromVer, int toVer)
    {
        var current = data;
        for (int v = fromVer; v < toVer; v++)
        {
            if (!_migrations.TryGetValue(v, out var migration))
                throw new MigrationNotFoundException(v, v + 1);
            current = migration.Migrate(current);
        }
        return current;
    }
}

// 使用示例
public class Migration_v1_to_v2 : IMigration
{
    public int FromVersion => 1;
    public int ToVersion => 2;

    public Dictionary<string, object> Migrate(Dictionary<string, object> data)
    {
        // 补充新增字段默认值
        if (!data.ContainsKey("party"))
            data["party"] = new Dictionary<string, object> { ["members"] = new List<string>() };
        return data;
    }
}
```

### 开发构建旁路

```csharp
#if DEBUG
    // 保存时额外写明文 JSON
    var devPath = savePath + ".dev.json";
    await File.WriteAllTextAsync(devPath, jsonString, Encoding.UTF8);
#endif
```

- `#if DEBUG` 编译时常量，正式构建完全不包含明文代码路径
- .dev.json 加入 .gitignore

### 自动存档触发

| 触发时机 | 延迟 | 目标槽 |
|---------|------|--------|
| 场景切换完成 | 500ms | autosave |
| 客栈休息完成 | 500ms | autosave |
| 主线章节节点推进后 | 500ms | autosave |
| 锁定对话序列完成后 | 500ms | autosave |

禁止触发条件：战斗中、过场动画中、锁定对话中、存档操作进行中。

## Alternatives Considered

### Alternative 1: GDExtension C++ 加密库

- **Description**: 用 C++ 实现加密模块，通过 GDExtension 暴露给 C#
- **Pros**: 可能更快；可选 libsodium 等成熟库
- **Cons**: 增加构建复杂度（需编译原生库）；跨平台构建负担；C#→C++ 互操作开销；团队无 C++ 经验
- **Rejection Reason**: .NET 8 的 System.Security.Cryptography 已经足够快（100KB 加密 < 1ms），不值得引入 GDExtension 复杂度

### Alternative 2: 第三方 NuGet (BouncyCastle)

- **Description**: 使用 BouncyCastle .NET 加密库
- **Pros**: 更多算法选择；Java 生态移植，文档丰富
- **Cons**: 额外大依赖（500KB+）；.NET 8 原生已完全覆盖 AES-256 需求
- **Rejection Reason**: System.Security.Cryptography 已足够，无需引入额外依赖

### Alternative 3: XOR 简单混淆（不加密）

- **Description**: 简单 XOR 混淆 + base64 编码
- **Pros**: 极简实现；零依赖
- **Cons**: 安全性极低（几分钟可破解）；无完整性校验
- **Rejection Reason**: 连"增加篡改难度"的目标都达不到；玩家社区会立即提供修改器

## Consequences

### Positive

- 使用 .NET 标准库 — 零额外依赖，跨平台兼容
- HMAC 完整性校验 — 检测存档损坏或篡改
- 后台写入 — 自动存档不卡顿
- 版本迁移链 — 游戏更新不破坏旧存档
- 开发旁路 — 调试效率不受加密影响

### Negative

- 密钥内置可执行文件 — 逆向可提取（单机游戏接受此限制）
- 加密增加存档体积 ~5%（padding + IV + HMAC = 64 bytes 固定开销）
- 迁移链错误可能导致存档不可恢复（需充分测试）

### Risks

| 风险 | 缓解 |
|------|------|
| 迁移函数 bug 损坏存档 | 迁移前备份原文件（.bak）；迁移超时 500ms 自动中断 |
| 加密密钥泄露 | 单机游戏接受；正式构建可选 code obfuscation |
| 后台线程写入失败（磁盘满） | SaveResult 返回错误信息；UI 提示玩家 |
| 存档文件被外部程序锁定 | 重试 3 次（间隔 100ms），失败后报错 |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| save-system.md | AES-256 对称密钥加密 | System.Security.Cryptography AES-256-CBC |
| save-system.md | 开发构建明文 dump | `#if DEBUG` 旁路写 .dev.json |
| save-system.md | 版本迁移链（schema_version 递增） | MigrationChain + IMigration 接口 |
| save-system.md | 10 手动槽 + 1 自动槽 | ISaveManager.ListSlots()，路径 slot_01~slot_10 + autosave |
| save-system.md | 存档元数据（时间戳、章节、天数、时长、缩略图） | SlotMetadata record |
| save-system.md | 自动存档触发（场景切换/休息/章节/对话后 500ms） | 延迟触发 + 禁止条件检查 |
| save-system.md | 不阻塞主线程 | async Task 后台写入 |
| save-system.md | 各系统注册序列化器 | ISaveable 接口 + SaveKey 注册模式 |

## Performance Implications

- **CPU**: AES-256 加密 100KB ≈ 0.5ms（.NET 8 hardware-accelerated AES-NI）
- **Memory**: 序列化峰值 ~1MB（JSON string + encrypted bytes 双缓冲）
- **Load Time**: 解密+反序列化+迁移 < 200ms（500KB 存档）
- **Network**: N/A（纯本地）

## Migration Plan

首次实现，无迁移需求。schema_version 从 1 开始。

## Validation Criteria

1. 单元测试：加密→解密 round-trip 数据一致
2. 单元测试：篡改密文 1 byte → HMAC 校验失败 → 拒绝加载
3. 单元测试：版本迁移链 v1→v3 正确执行两步迁移
4. 集成测试：SaveGame + LoadGame round-trip，各系统状态一致
5. 性能测试：500KB 存档 SaveGame < 50ms（含加密）
6. 性能测试：500KB 存档 LoadGame < 200ms（含解密+迁移）
7. 后台写入：SaveGame 期间主线程帧率不降

## Related Decisions

- [ADR-0001](adr-0001-event-bus-architecture.md) — SaveCompletedEvent / SaveLoadedEvent 通过 EventBus 发布
- [ADR-0003](adr-0003-data-configuration-format.md) — 配置数据用 YAML，存档数据用 JSON（运行时生成，无需注释）
- [architecture.md](architecture.md) — Section 6 DF-4 存档/读档数据流
