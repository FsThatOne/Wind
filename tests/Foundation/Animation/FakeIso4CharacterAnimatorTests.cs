using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.Fakes;
using Godot;
using Xunit;

namespace FengZhi.Tests.Foundation.Animation;

/// <summary>
/// ADR-0022 §2 4 斜方向选区 / 迟滞 / 帧相位 / Fake 计数验收测试（AC5）。
///
/// 8 个核心 case：
/// <list type="number">
///   <item><description>SetMovementVector_ResolvesFourSectors — 4 sector 正确性 (Theory 4 行)</description></item>
///   <item><description>WASD_CartDiagonals_FallInSectorCenters — §3 WASD 对角 → 4 sector 中心 (Theory 4 行)</description></item>
///   <item><description>NearSectorBoundary_KeepsCurrentWithin15DegHysteresis — ±15° 迟滞</description></item>
///   <item><description>OutsideHysteresis_SwitchesToNewSector — 越过迟滞 → 切换</description></item>
///   <item><description>SetMovementVector_Zero_PausesButKeepsDirection — 零向量 Pause 保 dir</description></item>
///   <item><description>Stop_ClearsCurrentDirection — Stop 清状态</description></item>
///   <item><description>SetMovementVector_RecordsLastVector — Fake 记最近向量</description></item>
///   <item><description>PlayCallCount_IncrementsEachCall — Fake 调用计数</description></item>
/// </list>
/// </summary>
public class FakeIso4CharacterAnimatorTests
{
    [Theory]
    [InlineData(0f, -1f, Iso4Direction.NE)]
    [InlineData(1f, 0f, Iso4Direction.SE)]
    [InlineData(0f, 1f, Iso4Direction.SW)]
    [InlineData(-1f, 0f, Iso4Direction.NW)]
    public void SetMovementVector_ResolvesFourSectors(float x, float y, Iso4Direction expected)
    {
        var anim = new FakeIso4CharacterAnimator();

        anim.SetMovementVector(new Vector2(x, y));

        Assert.Equal(expected, anim.CurrentDirection);
        Assert.True(anim.IsPlaying);
    }

    [Theory]
    [InlineData(-1f, -1f, Iso4Direction.NW)]
    [InlineData(+1f, -1f, Iso4Direction.NE)]
    [InlineData(+1f, +1f, Iso4Direction.SE)]
    [InlineData(-1f, +1f, Iso4Direction.SW)]
    public void WASD_CartDiagonals_FallInSectorCenters(float x, float y, Iso4Direction expected)
    {
        var anim = new FakeIso4CharacterAnimator();

        anim.SetMovementVector(new Vector2(x, y));

        Assert.Equal(expected, anim.CurrentDirection);
    }

    [Fact]
    public void NearSectorBoundary_KeepsCurrentWithin15DegHysteresis()
    {
        var anim = new Iso4HysteresisProbe();
        anim.SetMovementVector(new Vector2(+1f, -1f));
        Assert.Equal(Iso4Direction.NE, anim.CurrentDirection);

        // NE↔NW boundary at -3π/4. 10° past = -3π/4 - 10°.
        // Distance from NE center (-π/2): 45° + 10° = 55° < hysteresis limit (45° + 15° = 60°).
        var slightlyPastBoundary = AngleVector(-3f * Mathf.Pi / 4.0f - Mathf.DegToRad(10f));

        anim.SetMovementVector(slightlyPastBoundary);

        Assert.Equal(Iso4Direction.NE, anim.CurrentDirection);
    }

    [Fact]
    public void OutsideHysteresis_SwitchesToNewSector()
    {
        var anim = new Iso4HysteresisProbe();
        anim.SetMovementVector(new Vector2(+1f, -1f));
        Assert.Equal(Iso4Direction.NE, anim.CurrentDirection);

        // 20° past boundary at -3π/4. Distance from NE center = 65° > 60° → switch.
        var wellPastBoundary = AngleVector(-3f * Mathf.Pi / 4.0f - Mathf.DegToRad(20f));

        anim.SetMovementVector(wellPastBoundary);

        Assert.Equal(Iso4Direction.NW, anim.CurrentDirection);
    }

    [Fact]
    public void SetMovementVector_Zero_PausesButKeepsDirection()
    {
        var anim = new FakeIso4CharacterAnimator();
        anim.SetMovementVector(new Vector2(+1f, -1f));
        Assert.Equal(Iso4Direction.NE, anim.CurrentDirection);
        Assert.True(anim.IsPlaying);

        anim.SetMovementVector(Vector2.Zero);

        Assert.False(anim.IsPlaying);
        Assert.Equal(Iso4Direction.NE, anim.CurrentDirection);
        Assert.Equal(Vector2.Zero, anim.LastMovementVector);
    }

    [Fact]
    public void Stop_ClearsCurrentDirection()
    {
        var anim = new FakeIso4CharacterAnimator();
        anim.SetMovementVector(new Vector2(-1f, -1f));
        Assert.Equal(Iso4Direction.NW, anim.CurrentDirection);

        anim.Stop();

        Assert.Null(anim.CurrentDirection);
        Assert.Null(anim.Current);
        Assert.False(anim.IsPlaying);
    }

    [Fact]
    public void SetMovementVector_RecordsLastVector()
    {
        var anim = new FakeIso4CharacterAnimator();
        var v = new Vector2(0.7f, -0.3f);

        anim.SetMovementVector(v);

        Assert.Equal(v, anim.LastMovementVector);
    }

    [Fact]
    public void PlayCallCount_IncrementsEachCall()
    {
        var anim = new FakeIso4CharacterAnimator();

        anim.Play(CharacterAnimState.Idle);
        anim.Play(CharacterAnimState.Walk);
        anim.Play(CharacterAnimState.Attack);

        Assert.Equal(3, anim.PlayCallCount);
        Assert.Equal(CharacterAnimState.Attack, anim.Current);
    }

    private static Vector2 AngleVector(float angle) =>
        new(Mathf.Cos(angle), Mathf.Sin(angle));

    /// <summary>
    /// 在 Foundation 测试环境里复刻 <c>Iso4AnimatedSprite2DAnimator</c> 的迟滞算法，
    /// 不依赖 Godot Node / Sprite。仅供迟滞行为测试用，避免 Adapter 因为 Sprite 为 null
    /// 而 short-circuit。
    /// </summary>
    private sealed class Iso4HysteresisProbe
    {
        private const float SectorRadians = Mathf.Pi / 2.0f;
        private const float HysteresisRadians = Mathf.Pi / 12.0f;

        public Iso4Direction? CurrentDirection { get; private set; }

        public void SetMovementVector(Vector2 movement)
        {
            if (movement == Vector2.Zero) return;
            CurrentDirection = Resolve(movement, CurrentDirection);
        }

        private static Iso4Direction Resolve(Vector2 v, Iso4Direction? current)
        {
            var angle = Mathf.Atan2(v.Y, v.X);
            var sector = SectorFromAngle(angle);

            if (current is { } existing && existing != sector)
            {
                var existingCenter = SectorCenter(existing);
                var distanceFromExisting = Mathf.Abs(NormalizeAngle(angle - existingCenter));
                if (distanceFromExisting <= SectorRadians * 0.5f + HysteresisRadians)
                {
                    sector = existing;
                }
            }

            return sector;
        }

        private static Iso4Direction SectorFromAngle(float angle)
        {
            const float QuarterPi = Mathf.Pi / 4.0f;
            const float ThreeQuarterPi = 3.0f * Mathf.Pi / 4.0f;
            if (angle > ThreeQuarterPi || angle <= -ThreeQuarterPi) return Iso4Direction.NW;
            if (angle <= -QuarterPi) return Iso4Direction.NE;
            if (angle <= QuarterPi) return Iso4Direction.SE;
            return Iso4Direction.SW;
        }

        private static float SectorCenter(Iso4Direction direction) => direction switch
        {
            Iso4Direction.NE => -Mathf.Pi / 2.0f,
            Iso4Direction.SE => 0f,
            Iso4Direction.SW => +Mathf.Pi / 2.0f,
            Iso4Direction.NW => Mathf.Pi,
            _ => 0f,
        };

        private static float NormalizeAngle(float angle)
        {
            var twoPi = Mathf.Pi * 2.0f;
            angle = Mathf.PosMod(angle + Mathf.Pi, twoPi);
            return angle - Mathf.Pi;
        }
    }
}
