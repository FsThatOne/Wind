using FengZhi.Foundation.SceneManagement;
using Xunit;

namespace Foundation.Tests.SceneManagement;

public class WorldMapTravelTests
{
    private readonly TravelConfig _config = new();

    // --- AC1: CalcGridCost default values ---

    [Fact]
    public void CalcGridCost_WithStamina_ReturnsDefaultValues()
    {
        var (time, stamina) = WorldMapTravelCalculator.CalcGridCost(10f, _config);
        Assert.Equal(0.01f, time, 0.0001f);
        Assert.Equal(0.2f, stamina, 0.0001f);
    }

    // --- AC2: Speed modifier ---

    [Fact]
    public void CalcGridCost_ZeroStamina_TimeDoubled()
    {
        var (time, stamina) = WorldMapTravelCalculator.CalcGridCost(0f, _config);
        // time = 0.01 / 0.5 = 0.02
        Assert.Equal(0.02f, time, 0.0001f);
        Assert.Equal(0f, stamina); // no stamina cost when exhausted
    }

    [Fact]
    public void CalcGridCost_ZeroStamina_NoStaminaCost()
    {
        var (_, stamina) = WorldMapTravelCalculator.CalcGridCost(0f, _config);
        Assert.Equal(0f, stamina);
    }

    // --- AC3: Stagecoach formula ---

    [Fact]
    public void StagecoachTravel_CorrectTimeAndFee()
    {
        int distance = 10;
        var result = WorldMapTravelCalculator.StagecoachTravel(distance, 100f, _config);

        Assert.Equal(TravelResultType.Success, result.Type);
        // time = 10 * 0.01 * 0.5 = 0.05
        Assert.Equal(0.05f, result.TimeConsumed, 0.0001f);
        Assert.Equal(0f, result.StaminaConsumed);
        // fee = 10 + 2 * 10 = 30
        Assert.Equal(30f, result.Fee, 0.01f);
        Assert.Equal(10, result.GridsMoved);
    }

    [Fact]
    public void StagecoachTravel_NoStaminaCost()
    {
        var result = WorldMapTravelCalculator.StagecoachTravel(5, 100f, _config);
        Assert.Equal(0f, result.StaminaConsumed);
    }

    // --- AC4: Multi-grid walk accumulation ---

    [Fact]
    public void WalkTravel_MultiGrid_AccumulatesCorrectly()
    {
        // 5 grids with plenty of stamina
        var result = WorldMapTravelCalculator.WalkTravel(5, 10f, _config);
        Assert.Equal(TravelResultType.Success, result.Type);
        // time = 5 * 0.01 = 0.05
        Assert.Equal(0.05f, result.TimeConsumed, 0.0001f);
        // stamina = 5 * 0.2 = 1.0
        Assert.Equal(1.0f, result.StaminaConsumed, 0.0001f);
        Assert.Equal(5, result.GridsMoved);
    }

    [Fact]
    public void WalkTravel_StaminaRunsOut_SlowsDown()
    {
        // Start with 0.3 stamina, walk 3 grids
        // Grid 1: stamina=0.3>0, time=0.01, cost=0.2, remaining=0.1
        // Grid 2: stamina=0.1>0, time=0.01, cost=0.2, remaining=-0.1→0
        // Grid 3: stamina=0, time=0.02, cost=0
        var result = WorldMapTravelCalculator.WalkTravel(3, 0.3f, _config);
        Assert.Equal(0.01f + 0.01f + 0.02f, result.TimeConsumed, 0.0001f);
        Assert.Equal(0.2f + 0.2f + 0f, result.StaminaConsumed, 0.0001f);
    }

    // --- AC5: CannotAfford ---

    [Fact]
    public void StagecoachTravel_InsufficientFunds_CannotAfford()
    {
        // fee = 10 + 2*5 = 20, only have 15
        var result = WorldMapTravelCalculator.StagecoachTravel(5, 15f, _config);
        Assert.Equal(TravelResultType.CannotAfford, result.Type);
        Assert.Equal(20f, result.Fee, 0.01f);
        Assert.Equal(0, result.GridsMoved);
    }

    [Fact]
    public void StagecoachTravel_ExactFunds_Succeeds()
    {
        // fee = 10 + 2*5 = 20, have exactly 20
        var result = WorldMapTravelCalculator.StagecoachTravel(5, 20f, _config);
        Assert.Equal(TravelResultType.Success, result.Type);
    }

    // --- AC6: Config parameters respected ---

    [Fact]
    public void WalkTravel_CustomConfig_Respected()
    {
        var config = new TravelConfig { TimePerGrid = 0.02f, StaminaPerGrid = 0.5f };
        var result = WorldMapTravelCalculator.WalkTravel(2, 10f, config);
        Assert.Equal(0.04f, result.TimeConsumed, 0.0001f);
        Assert.Equal(1.0f, result.StaminaConsumed, 0.0001f);
    }

    [Fact]
    public void StagecoachTravel_CustomConfig_Respected()
    {
        var config = new TravelConfig
        {
            TimePerGrid = 0.02f,
            StationTimeMult = 0.3f,
            StationBaseFee = 5f,
            StationDistanceFee = 1f
        };
        // time = 4 * 0.02 * 0.3 = 0.024
        // fee = 5 + 1 * 4 = 9
        var result = WorldMapTravelCalculator.StagecoachTravel(4, 50f, config);
        Assert.Equal(0.024f, result.TimeConsumed, 0.0001f);
        Assert.Equal(9f, result.Fee, 0.01f);
    }
}
