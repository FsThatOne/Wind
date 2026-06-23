using FengZhi.Foundation.Geometry;
using Godot;
using Xunit;

namespace FengZhi.Tests.Foundation.Geometry;

public class IsoProjectionTests
{
    private const float Eps = 1e-4f;

    [Fact]
    public void Origin_RoundTripsToZero()
    {
        var cart = Vector2.Zero;

        var screen = IsoProjection.CartToScreen(cart);
        var roundTrip = IsoProjection.ScreenToCart(screen);

        Assert.Equal(0f, screen.X, Eps);
        Assert.Equal(0f, screen.Y, Eps);
        Assert.Equal(0f, roundTrip.X, Eps);
        Assert.Equal(0f, roundTrip.Y, Eps);
    }

    [Theory]
    [InlineData(1f, 0f, +32f, +16f)]
    [InlineData(0f, 1f, -32f, +16f)]
    [InlineData(1f, 1f, 0f, +32f)]
    [InlineData(-1f, -1f, 0f, -32f)]
    public void CartToScreen_MatchesAdrFormula(float cx, float cy, float sx, float sy)
    {
        var screen = IsoProjection.CartToScreen(new Vector2(cx, cy));

        Assert.Equal(sx, screen.X, Eps);
        Assert.Equal(sy, screen.Y, Eps);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(2.5f, -1.25f)]
    [InlineData(-3f, +4f)]
    [InlineData(0.5f, 0.5f)]
    public void RoundTrip_CartToScreenToCart_IsInvariant(float cx, float cy)
    {
        var cart = new Vector2(cx, cy);

        var screen = IsoProjection.CartToScreen(cart);
        var back = IsoProjection.ScreenToCart(screen);

        Assert.Equal(cart.X, back.X, Eps);
        Assert.Equal(cart.Y, back.Y, Eps);
    }

    [Fact]
    public void TileConstants_MatchAdr0022Default()
    {
        Assert.Equal(64f, IsoProjection.TileWidth);
        Assert.Equal(32f, IsoProjection.TileHeight);
        Assert.Equal(2f, IsoProjection.TileWidth / IsoProjection.TileHeight);
    }
}
