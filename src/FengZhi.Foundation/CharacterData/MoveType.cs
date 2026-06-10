namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 招式体系类型。
/// </summary>
public enum MoveType
{
    /// <summary>刚 — 主属性: 力量</summary>
    Gang,
    /// <summary>巧 — 主属性: 敏捷</summary>
    Qiao,
    /// <summary>柔 — 主属性: 内力</summary>
    Rou
}

/// <summary>
/// 相对强度比较结果。
/// </summary>
public enum PowerLevel
{
    /// <summary>ratio &lt; 0.5 — "此人气势如渊，你感到难以抗衡"</summary>
    FarWeaker,
    /// <summary>0.5 ≤ ratio &lt; 0.8 — "此人内力深厚，不可小觑"</summary>
    Weaker,
    /// <summary>0.8 ≤ ratio &lt; 1.2 — "你与此人旗鼓相当"</summary>
    Comparable,
    /// <summary>1.2 ≤ ratio &lt; 2.0 — "此人功力尚浅"</summary>
    Stronger,
    /// <summary>ratio ≥ 2.0 — "此人不足为惧"</summary>
    FarStronger
}
