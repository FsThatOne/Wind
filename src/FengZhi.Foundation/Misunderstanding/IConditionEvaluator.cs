using System.Collections.Generic;

namespace FengZhi.Foundation.Misunderstanding;

/// <summary>条件评估器接口 — 判断误会的解除条件是否全部满足。</summary>
public interface IConditionEvaluator
{
    bool AreConditionsMet(List<string> conditionIds);
}
