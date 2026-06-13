namespace FengZhi.Foundation.Combat.AI;

/// <summary>
/// AI 决策的随机源接口。测试时注入固定/seeded 实例。
/// </summary>
public interface IAIRandomSource
{
    /// <summary>
    /// 返回 [0, 1) 范围内的值，用于体系选择等概率判定。
    /// </summary>
    float NextFloat();
}

/// <summary>
/// 基于 System.Random 的默认 AI 随机源。
/// </summary>
public sealed class DefaultAIRandom : IAIRandomSource
{
    private readonly Random _rng;
    public DefaultAIRandom(int? seed = null) => _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    public float NextFloat() => (float)_rng.NextDouble();
}

/// <summary>
/// 固定序列随机源，用于测试确定性。
/// </summary>
public sealed class SeededAIRandom : IAIRandomSource
{
    private readonly Random _rng;
    public SeededAIRandom(int seed) => _rng = new Random(seed);
    public float NextFloat() => (float)_rng.NextDouble();
}
