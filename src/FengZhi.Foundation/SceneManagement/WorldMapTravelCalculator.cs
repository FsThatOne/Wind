namespace FengZhi.Foundation.SceneManagement;

public sealed class TravelConfig
{
    public float TimePerGrid { get; set; } = 0.01f;
    public float StaminaPerGrid { get; set; } = 0.2f;
    public float ZeroStaminaSpeedMult { get; set; } = 0.5f;
    public float StationTimeMult { get; set; } = 0.5f;
    public float StationBaseFee { get; set; } = 10f;
    public float StationDistanceFee { get; set; } = 2f;
}

public enum TravelResultType { Success, CannotAfford }

public sealed class TravelResult
{
    public TravelResultType Type { get; init; } = TravelResultType.Success;
    public float TimeConsumed { get; init; }
    public float StaminaConsumed { get; init; }
    public float Fee { get; init; }
    public int GridsMoved { get; init; }
}

public static class WorldMapTravelCalculator
{
    /// <summary>
    /// Calculate cost for moving a single grid.
    /// Returns effective time (doubled if exhausted) and stamina cost.
    /// </summary>
    public static (float time, float stamina) CalcGridCost(float currentStamina, TravelConfig config)
    {
        float speedMult = currentStamina > 0 ? 1f : config.ZeroStaminaSpeedMult;
        float effectiveTime = config.TimePerGrid / speedMult;
        float staminaCost = currentStamina > 0 ? config.StaminaPerGrid : 0f;
        return (effectiveTime, staminaCost);
    }

    /// <summary>
    /// Simulate multi-grid walking travel, accumulating time and stamina per grid.
    /// </summary>
    public static TravelResult WalkTravel(int distance, float currentStamina, TravelConfig config)
    {
        float totalTime = 0f;
        float totalStamina = 0f;
        float stamina = currentStamina;

        for (int i = 0; i < distance; i++)
        {
            var (time, cost) = CalcGridCost(stamina, config);
            totalTime += time;
            totalStamina += cost;
            stamina -= cost;
            if (stamina < 0) stamina = 0;
        }

        return new TravelResult
        {
            Type = TravelResultType.Success,
            TimeConsumed = totalTime,
            StaminaConsumed = totalStamina,
            GridsMoved = distance
        };
    }

    /// <summary>
    /// Calculate stagecoach (驿站) travel.
    /// Time = distance * timePerGrid * stationTimeMult, stamina = 0, fee = base + distance * rate
    /// </summary>
    public static TravelResult StagecoachTravel(int distance, float currentMoney, TravelConfig config)
    {
        float fee = config.StationBaseFee + config.StationDistanceFee * distance;
        if (currentMoney < fee)
        {
            return new TravelResult
            {
                Type = TravelResultType.CannotAfford,
                Fee = fee,
                GridsMoved = 0
            };
        }

        float time = distance * config.TimePerGrid * config.StationTimeMult;

        return new TravelResult
        {
            Type = TravelResultType.Success,
            TimeConsumed = time,
            StaminaConsumed = 0f,
            Fee = fee,
            GridsMoved = distance
        };
    }
}
