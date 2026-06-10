# Test Infrastructure

**Engine**: Godot 4.6.3
**Language**: C# (.NET 8+) + GDScript (混合模式)
**Test Framework**: GdUnit4（同时支持 C# 与 GDScript）
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-06-09

## Directory Layout

```
tests/
  unit/           # 隔离的单元测试（公式、状态机、逻辑）
  integration/    # 跨系统与存档往返测试
  smoke/          # /smoke-check gate 用的关键路径清单
  evidence/       # 截图日志与手动测试签字记录
  gdunit4_runner.gd  # CLI / CI 入口
```

## Running Tests

### 本地（Godot 编辑器）

1. 安装 GdUnit4 插件（仅一次）：
   - Godot → AssetLib → 搜索 `GdUnit4` → Download & Install
   - Project → Project Settings → Plugins → 勾选 GdUnit4 → 重启编辑器
   - 验证：`res://addons/gdunit4/` 存在
2. 在 Godot 编辑器内右键 `tests/unit` 或 `tests/integration` → Run Tests

### 本地（CLI / Headless）

```bash
godot --headless --script tests/gdunit4_runner.gd
```

### CI

每次推送或 PR 至 `main` 时自动运行（见 `.github/workflows/tests.yml`）。

## Test Naming

- **C# 文件**：`[System][Feature]Test.cs`（PascalCase，匹配项目编码规范）
- **GDScript 文件**：`[system]_[feature]_test.gd`（snake_case）
- **测试方法**：`Test[Scenario]_[Expected]`（C#）/ `test_[scenario]_[expected]`（GDScript）
- **示例**：
  - `CombatDamageTest.cs` → `TestBaseAttack_ReturnsExpectedDamage()`
  - `combat_damage_test.gd` → `test_base_attack_returns_expected_damage()`

## C# 测试编写要点

- 测试类必须声明为 `partial`（与 Godot 源生成器约束一致）
- 使用 GdUnit4 的 `[TestSuite]` / `[TestCase]` 特性
- 断言使用 `AssertThat(...)`、`AssertString(...)` 等流式 API

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class CombatDamageTest
{
    [TestCase]
    public void TestBaseAttack_ReturnsExpectedDamage()
    {
        AssertThat(10 + 5).IsEqual(15);
    }
}
```

## Story Type → Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | 自动化单元测试（必须通过） | `tests/unit/[system]/` |
| Integration | 集成测试或 playtest 文档 | `tests/integration/[system]/` |
| Visual/Feel | 截图 + Lead 签字 | `tests/evidence/` |
| UI | 手动 walkthrough 或交互测试 | `tests/evidence/` |
| Config/Data | smoke check 通过 | `production/qa/smoke-*.md` |

## CI

- 每次 push 至 `main` 与每个 PR 自动运行测试套件
- 测试失败将阻塞 merge
- 报告产物上传至 GitHub Actions artifacts (`test-results`)
