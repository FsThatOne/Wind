namespace FengZhi.Foundation.MartialArts;

/// <summary>
/// 功力境界等级。影响招式缩放系数。
/// </summary>
public enum RealmTier
{
    /// <summary>初学乍练 ×0.7</summary>
    ChuXueZhaLian,

    /// <summary>初窥门径 ×0.8</summary>
    ChuKuiMenJing,

    /// <summary>登堂入室 ×0.9</summary>
    DengTangRuShi,

    /// <summary>融会贯通 ×1.0</summary>
    RongHuiGuanTong,

    /// <summary>驾轻就熟 ×1.1</summary>
    JiaQingJiuShu,

    /// <summary>炉火纯青 ×1.3</summary>
    LuHuoChunQing,

    /// <summary>出神入化 ×1.5</summary>
    ChuShenRuHua,

    /// <summary>登峰造极 ×1.8</summary>
    DengFengZaoJi,

    /// <summary>返璞归真 ×2.0</summary>
    FanPuGuiZhen
}

/// <summary>
/// 境界缩放系数表。集中定义，避免魔法数字散落。
/// </summary>
public static class RealmScaling
{
    private static readonly Dictionary<RealmTier, float> Table = new()
    {
        [RealmTier.ChuXueZhaLian] = 0.7f,
        [RealmTier.ChuKuiMenJing] = 0.8f,
        [RealmTier.DengTangRuShi] = 0.9f,
        [RealmTier.RongHuiGuanTong] = 1.0f,
        [RealmTier.JiaQingJiuShu] = 1.1f,
        [RealmTier.LuHuoChunQing] = 1.3f,
        [RealmTier.ChuShenRuHua] = 1.5f,
        [RealmTier.DengFengZaoJi] = 1.8f,
        [RealmTier.FanPuGuiZhen] = 2.0f,
    };

    /// <summary>
    /// 获取境界对应的缩放系数。非法境界抛出异常。
    /// </summary>
    public static float GetScaling(RealmTier realm)
    {
        if (!Table.TryGetValue(realm, out var scaling))
            throw new ArgumentOutOfRangeException(nameof(realm), realm, "未知境界等级");
        return scaling;
    }
}
