# Story: ss-003 — 存档加密、HMAC 与文件头校验

> **Epic**: save-system
> **类型**: Logic
> **优先级**: P0 — 存档安全与完整性
> **依赖**: ss-001
> **阻塞**: 无
> **ADR 指引**: ADR-0004（AES-256-CBC、HMAC-SHA256、文件头）
> **GDD 来源**: design/gdd/save-system.md §Detailed Design, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现存档文件的加密、完整性校验和文件头读取，让正式构建的存档不能被简单篡改。

## 范围

### 包含
- AES-256-CBC + PKCS7 加密
- 每次加密随机生成 16 bytes IV
- HMAC-SHA256 完整性校验
- `"FZHI"` 文件头校验
- `schema_version` 读取和基础合法性验证
- 篡改密文时拒绝加载

### 不包含
- 迁移链 → ss-004
- 文件读写管理 → ss-002
- 明文调试旁路 → ss-004

## 技术说明

- 必须使用 `.NET 8 System.Security.Cryptography`
- 不能引入 GDExtension C++ 或 BouncyCastle
- 存档操作不可阻塞主线程的工作流在 ss-002 / ss-005 中承接

## 验收标准

- [x] 正式构建中，存档数据以 AES-256-CBC 加密
- [x] 每次加密生成随机 IV，并写入密文头部
- [x] 文件头 magic 不正确时拒绝加载
- [x] HMAC 校验失败时拒绝加载
- [x] 篡改 1 byte 也能被检测出来

## QA 测试用例

- **AC-1**：加密往返
  - Given：一份简单 JSON payload
  - When：执行加密再解密
  - Then：还原后的数据与原数据一致
  - 边界：空 payload、较大 payload

- **AC-2**：篡改检测
  - Given：一个合法存档文件
  - When：修改密文中任意 1 byte
  - Then：HMAC 校验失败，加载被拒绝
  - 边界：修改 header、IV、payload 任一部分都应失败

- **AC-3**：头部校验
  - Given：magic 非 `"FZHI"` 的文件
  - When：尝试加载
  - Then：直接拒绝，不进入解密流程
  - 边界：schema_version 高于当前版本时由 ss-004 处理

## 测试证据路径

`tests/unit/save-system/save-crypto_test.cs`

## 实现记录

- 新增 `SaveCryptoService`，负责 JSON payload 的 AES-256-CBC 加密、PKCS7 padding、随机 IV 生成和 HMAC-SHA256 完整性校验。
- 新增 `SaveDecryptResult`，以显式结果对象承载解密成功、header、JSON 与错误原因，避免上层误把失败结果当作合法存档。
- 扩展 `SaveFileHeader` 的 32 bytes 二进制头部序列化与反序列化，包含 `"FZHI"` magic、`schema_version`、flags 与 reserved 区。
- 存档二进制格式为 `Header + IV + Ciphertext + HMAC`，HMAC 覆盖 `Header + IV + Ciphertext`。
- 当前 story 不处理文件系统、迁移链和 DEBUG 明文 dump；这些边界继续归属 `ss-002` 与 `ss-004`。

## 验证结果

- 2026-06-11：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`
- 结果：通过 533，失败 0，跳过 0。
- 备注：测试输出存在既有 `SceneConfigTests.cs` 可空警告，与本 story 无关。

## 依赖关系

- Depends on: ss-001
- Unlocks: ss-002, ss-004, ss-005
