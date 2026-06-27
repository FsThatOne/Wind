using Xunit;
using FengZhi.Foundation.Audio;

namespace FengZhi.Tests.Audio;

/// <summary>
/// Story 005: SFX 优先级仲裁 + 并发池单元测试。
/// 测试 SfxPoolEngine 纯逻辑（无 Godot 依赖）。
/// </summary>
public class SfxPoolTest
{
    private readonly SfxPoolEngine _engine = new();

    // === AC7: SFX 洪水 — P0+P1 全部播放，按优先级填充 ===

    [Fact]
    public void AC7_FloodTest_P0P1AllPlay_P2FillsRemaining()
    {
        float t = 1000f;

        // 2×P0
        var r1 = _engine.TryPlay("sys_confirm", SfxPriority.P0, "ui", t);
        var r2 = _engine.TryPlay("sys_error", SfxPriority.P0, "ui2", t + 1);
        Assert.Equal(PlaySfxResult.Played, r1.Result);
        Assert.Equal(PlaySfxResult.Played, r2.Result);

        // 3×P1
        var r3 = _engine.TryPlay("hit_burst", SfxPriority.P1, "player", t + 2);
        var r4 = _engine.TryPlay("hit_read", SfxPriority.P1, "enemy1", t + 3);
        var r5 = _engine.TryPlay("hit_dmg", SfxPriority.P1, "enemy2", t + 4);
        Assert.Equal(PlaySfxResult.Played, r3.Result);
        Assert.Equal(PlaySfxResult.Played, r4.Result);
        Assert.Equal(PlaySfxResult.Played, r5.Result);

        // 4×P2 — 只有前 3 个能填满 8 路
        var r6 = _engine.TryPlay("sword_slash1", SfxPriority.P2, "player2", t + 5);
        var r7 = _engine.TryPlay("sword_slash2", SfxPriority.P2, "enemy3", t + 6);
        var r8 = _engine.TryPlay("sword_slash3", SfxPriority.P2, "enemy4", t + 7);
        Assert.Equal(PlaySfxResult.Played, r6.Result);
        Assert.Equal(PlaySfxResult.Played, r7.Result);
        Assert.Equal(PlaySfxResult.Played, r8.Result);

        // 第 9 个 P2 — 8 路满，无法淘汰（同优先级不淘汰）
        var r9 = _engine.TryPlay("sword_slash4", SfxPriority.P2, "enemy5", t + 8);
        Assert.Equal(PlaySfxResult.Rejected, r9.Result);

        // 3×P4 — 全部被拒绝
        var r10 = _engine.TryPlay("wind1", SfxPriority.P4, "env1", t + 9);
        var r11 = _engine.TryPlay("wind2", SfxPriority.P4, "env2", t + 10);
        var r12 = _engine.TryPlay("wind3", SfxPriority.P4, "env3", t + 11);
        Assert.Equal(PlaySfxResult.Rejected, r10.Result);
        Assert.Equal(PlaySfxResult.Rejected, r11.Result);
        Assert.Equal(PlaySfxResult.Rejected, r12.Result);

        Assert.Equal(8, _engine.ActiveCount);
    }

    // === 8 路满时淘汰最低优先级 ===

    [Fact]
    public void PoolFull_EvictsLowestPriority()
    {
        float t = 1000f;

        // 填满 8 路：7×P3 + 1×P4
        for (int i = 0; i < 7; i++)
            _engine.TryPlay($"env_{i}", SfxPriority.P3, $"src_{i}", t + i);
        _engine.TryPlay("wind_bg", SfxPriority.P4, "src_7", t + 7);

        Assert.Equal(8, _engine.ActiveCount);

        // P2 请求 → 应淘汰 P4 slot
        var result = _engine.TryPlay("slash", SfxPriority.P2, "player", t + 8);
        Assert.Equal(PlaySfxResult.Played, result.Result);
        Assert.True(result.EvictedSlotIndex >= 0);

        // 被淘汰的 slot 原来是 P4
        Assert.Equal(8, _engine.ActiveCount);
    }

    [Fact]
    public void PoolFull_CannotEvictSameOrHigherPriority()
    {
        float t = 1000f;

        // 填满 8 路全部 P2
        for (int i = 0; i < 8; i++)
            _engine.TryPlay($"slash_{i}", SfxPriority.P2, $"src_{i}", t + i);

        // 同优先级 P2 不能淘汰 P2
        var result = _engine.TryPlay("slash_new", SfxPriority.P2, "player_new", t + 100);
        Assert.Equal(PlaySfxResult.Rejected, result.Result);

        // P3 也不能淘汰 P2
        var result2 = _engine.TryPlay("step", SfxPriority.P3, "npc", t + 101);
        Assert.Equal(PlaySfxResult.Rejected, result2.Result);
    }

    // === E6: P0/P1 临时突破 8 路上限 ===

    [Fact]
    public void E6_P0P1_OverflowBeyond8()
    {
        float t = 1000f;

        // 填满 8 路常规池
        for (int i = 0; i < 8; i++)
            _engine.TryPlay($"env_{i}", SfxPriority.P3, $"src_{i}", t + i);

        Assert.Equal(8, _engine.ActiveCount);

        // P0 应使用溢出 slot
        var r1 = _engine.TryPlay("sys_critical", SfxPriority.P0, "system", t + 100);
        Assert.Equal(PlaySfxResult.Played, r1.Result);
        Assert.True(r1.SlotIndex >= SfxPoolEngine.NormalPoolSize);
        Assert.Equal(9, _engine.ActiveCount);

        // P1 也能溢出
        var r2 = _engine.TryPlay("hit_critical", SfxPriority.P1, "combat", t + 101);
        Assert.Equal(PlaySfxResult.Played, r2.Result);
        Assert.True(r2.SlotIndex >= SfxPoolEngine.NormalPoolSize);
        Assert.Equal(10, _engine.ActiveCount);
    }

    [Fact]
    public void E6_P2_CannotOverflow_MustEvict()
    {
        float t = 1000f;

        // 填满 8 路 P3
        for (int i = 0; i < 8; i++)
            _engine.TryPlay($"step_{i}", SfxPriority.P3, $"src_{i}", t + i);

        // P2 不能溢出，但可以淘汰 P3
        var result = _engine.TryPlay("slash", SfxPriority.P2, "player", t + 100);
        Assert.Equal(PlaySfxResult.Played, result.Result);
        Assert.True(result.SlotIndex < SfxPoolEngine.NormalPoolSize);
        Assert.True(result.EvictedSlotIndex >= 0);
    }

    // === 同一 SFX 50ms cooldown ===

    [Fact]
    public void Cooldown_SameSfx_Within50ms_Blocked()
    {
        float t = 1000f;

        var r1 = _engine.TryPlay("hit_gang", SfxPriority.P2, "player", t);
        Assert.Equal(PlaySfxResult.Played, r1.Result);

        // 30ms 后再次请求 — 应被拒绝
        var r2 = _engine.TryPlay("hit_gang", SfxPriority.P2, "player2", t + 30f);
        Assert.Equal(PlaySfxResult.CooldownBlocked, r2.Result);
    }

    [Fact]
    public void Cooldown_SameSfx_After50ms_Allowed()
    {
        float t = 1000f;

        _engine.TryPlay("hit_gang", SfxPriority.P2, "player", t);

        // 50ms 后允许
        var result = _engine.TryPlay("hit_gang", SfxPriority.P2, "player2", t + 50f);
        Assert.Equal(PlaySfxResult.Played, result.Result);
    }

    [Fact]
    public void Cooldown_DifferentSfx_NotAffected()
    {
        float t = 1000f;

        _engine.TryPlay("hit_gang", SfxPriority.P2, "player", t);

        // 不同 sfxId 不受影响
        var result = _engine.TryPlay("hit_slash", SfxPriority.P2, "player2", t + 10f);
        Assert.Equal(PlaySfxResult.Played, result.Result);
    }

    // === 同源限制 2 路 ===

    [Fact]
    public void SameSource_MaxTwo_ThirdBlocked()
    {
        float t = 1000f;

        var r1 = _engine.TryPlay("step1", SfxPriority.P3, "player_protagonist", t);
        var r2 = _engine.TryPlay("step2", SfxPriority.P3, "player_protagonist", t + 1);
        Assert.Equal(PlaySfxResult.Played, r1.Result);
        Assert.Equal(PlaySfxResult.Played, r2.Result);

        // 第 3 个同源请求 — 被拒绝
        var r3 = _engine.TryPlay("step3", SfxPriority.P3, "player_protagonist", t + 2);
        Assert.Equal(PlaySfxResult.SameSourceLimitReached, r3.Result);
    }

    [Fact]
    public void SameSource_AfterSlotFinished_Allowed()
    {
        float t = 1000f;

        var r1 = _engine.TryPlay("step1", SfxPriority.P3, "player_protagonist", t);
        _engine.TryPlay("step2", SfxPriority.P3, "player_protagonist", t + 1);

        // 第 1 路结束
        _engine.MarkSlotFinished(r1.SlotIndex);

        // 现在只有 1 路活跃，第 3 个请求应该被允许
        var r3 = _engine.TryPlay("step3", SfxPriority.P3, "player_protagonist", t + 100);
        Assert.Equal(PlaySfxResult.Played, r3.Result);
    }

    [Fact]
    public void DifferentSource_NotAffected()
    {
        float t = 1000f;

        _engine.TryPlay("step1", SfxPriority.P3, "player_protagonist", t);
        _engine.TryPlay("step2", SfxPriority.P3, "player_protagonist", t + 1);

        // 不同 sourceId 不受限制
        var result = _engine.TryPlay("step3", SfxPriority.P3, "enemy_npc", t + 2);
        Assert.Equal(PlaySfxResult.Played, result.Result);
    }

    // === MarkSlotFinished 回收 ===

    [Fact]
    public void MarkSlotFinished_FreesSlot()
    {
        float t = 1000f;

        var r = _engine.TryPlay("hit", SfxPriority.P2, "player", t);
        Assert.Equal(1, _engine.ActiveCount);

        _engine.MarkSlotFinished(r.SlotIndex);
        Assert.Equal(0, _engine.ActiveCount);
    }

    // === Constants ===

    [Fact]
    public void Constants_MatchGddSpec()
    {
        Assert.Equal(8, SfxPoolEngine.NormalPoolSize);
        Assert.Equal(12, SfxPoolEngine.MaxPoolSize);
        Assert.Equal(50f, SfxPoolEngine.CooldownMs);
        Assert.Equal(2, SfxPoolEngine.MaxPerSource);
    }

    // === Reset ===

    [Fact]
    public void Reset_ClearsAllSlotsAndCooldowns()
    {
        float t = 1000f;
        _engine.TryPlay("hit", SfxPriority.P2, "player", t);
        _engine.TryPlay("slash", SfxPriority.P2, "enemy", t + 1);

        _engine.Reset();

        Assert.Equal(0, _engine.ActiveCount);

        // Cooldown 也被清除 — 可以立即再次播放
        var result = _engine.TryPlay("hit", SfxPriority.P2, "player", t + 10);
        Assert.Equal(PlaySfxResult.Played, result.Result);
    }
}
