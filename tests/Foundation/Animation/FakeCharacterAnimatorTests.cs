using FengZhi.Foundation.Animation;
using FengZhi.Foundation.Animation.Fakes;
using Xunit;

namespace FengZhi.Tests.Foundation.Animation;

public class FakeCharacterAnimatorTests
{
    [Fact]
    public void Play_SetsCurrentAndIsPlaying()
    {
        var anim = new FakeCharacterAnimator();

        anim.Play(CharacterAnimState.Attack, loop: false);

        Assert.Equal(CharacterAnimState.Attack, anim.Current);
        Assert.True(anim.IsPlaying);
        Assert.False(anim.LoopRequested);
        Assert.Equal(0f, anim.NormalizedProgress);
        Assert.Equal(1, anim.PlayCallCount);
    }

    [Fact]
    public void Play_DefaultLoopIsTrue()
    {
        var anim = new FakeCharacterAnimator();

        anim.Play(CharacterAnimState.Idle);

        Assert.True(anim.LoopRequested);
    }

    [Fact]
    public void Stop_ClearsState()
    {
        var anim = new FakeCharacterAnimator();
        anim.Play(CharacterAnimState.Walk);

        anim.Stop();

        Assert.False(anim.IsPlaying);
        Assert.Null(anim.Current);
        Assert.Equal(0f, anim.NormalizedProgress);
    }

    [Fact]
    public void EmitFinished_RaisesEventAndStops()
    {
        var anim = new FakeCharacterAnimator();
        CharacterAnimState? received = null;
        anim.Finished += s => received = s;

        anim.Play(CharacterAnimState.Hurt, loop: false);
        anim.EmitFinished(CharacterAnimState.Hurt);

        Assert.Equal(CharacterAnimState.Hurt, received);
        Assert.False(anim.IsPlaying);
        Assert.Equal(1f, anim.NormalizedProgress);
    }

    [Fact]
    public void SetFacing_UpdatesFacing()
    {
        var anim = new FakeCharacterAnimator();

        anim.SetFacing(Facing.Left);

        Assert.Equal(Facing.Left, anim.Facing);
    }

    [Fact]
    public void EmitFrameEvent_ForwardsTagAndProgress()
    {
        var anim = new FakeCharacterAnimator();
        AnimationFrameEvent? captured = null;
        anim.FrameEvent += e => captured = e;

        anim.EmitFrameEvent("hit", 0.5f);

        Assert.NotNull(captured);
        Assert.Equal("hit", captured!.Value.Tag);
        Assert.Equal(0.5f, captured.Value.Progress);
    }

    [Fact]
    public void SetProgress_ClampsTo0And1()
    {
        var anim = new FakeCharacterAnimator();

        anim.SetProgress(-0.5f);
        Assert.Equal(0f, anim.NormalizedProgress);

        anim.SetProgress(1.5f);
        Assert.Equal(1f, anim.NormalizedProgress);

        anim.SetProgress(0.4f);
        Assert.Equal(0.4f, anim.NormalizedProgress);
    }

    [Fact]
    public void PlayCallCount_IncrementsEachCall()
    {
        var anim = new FakeCharacterAnimator();

        anim.Play(CharacterAnimState.Idle);
        anim.Play(CharacterAnimState.Walk);
        anim.Play(CharacterAnimState.Attack);

        Assert.Equal(3, anim.PlayCallCount);
        Assert.Equal(CharacterAnimState.Attack, anim.Current);
    }
}
