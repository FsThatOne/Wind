namespace FengZhi.Foundation.Mindset;

/// <summary>
/// 心境双轴 9 区域 + 5 善恶档位的中文文学化名称查找表。
///
/// 设计哲学（blurred-ui Rule 1-3）：
/// - 战斗外仅以文学名 + 短描述呈现，不暴露数值
/// - 9 区域名取自 GDD §双轴区域命名表（孤剑入世 / 风止沉烟 / 大隐于市 等）
/// - 善恶档位以"道义"维度文学化（恶贯满盈 / 浊世染身 / 常人 / 仁善有声 / 光风霁月）
///
/// 与 <see cref="MindsetPresentationService"/> 关系：
/// - Presentation 输出"轴向描述"（如"旧恨如铁，尚未松手"）
/// - 本类输出"zone 名"（如"孤剑入世"）与"道义档位名"
/// - OutcomeGame UI 同时使用两者拼出朦胧化战后面板
///
/// Sprint 7 VS S7-VS-Outcome-Feedback 引入；Sprint 8 blurred-ui epic 起步时可收编
/// 到独立 BlurredUiContent 数据资源（YAML / .tres）。当前阶段以代码常量形态足够。
/// </summary>
public static class MindsetZoneLiteraryNames
{
    /// <summary>
    /// 9 区域中文文学化名 + 区域副标（短意境描述）。
    /// </summary>
    public static (string Name, string Subtitle) GetZoneName(MindsetZone zone)
    {
        return zone switch
        {
            MindsetZone.GuJianRuShi => ("孤剑入世", "孤剑独行，血未冷透"),
            MindsetZone.ZhiNianWeiDing => ("执念未定", "执念如刃，未择其向"),
            MindsetZone.FengZhiChenYan => ("风止沉烟", "执念深处，江湖渐远"),
            MindsetZone.RuShiWeiDing => ("入世未定", "踏入红尘，未择其路"),
            MindsetZone.ZhongYong => ("中庸", "心如平湖，去留两忘"),
            MindsetZone.ChuShiWeiDing => ("出世未定", "渐疏尘嚣，未择其归"),
            MindsetZone.BaiYiRuShi => ("白衣入世", "释然在心，剑指人间"),
            MindsetZone.ShiHuaiWeiDing => ("释怀未定", "心结渐松，去留两可"),
            MindsetZone.DaYinYuShi => ("大隐于市", "归意已定，于人间静处"),
            _ => ("未知", "—"),
        };
    }

    /// <summary>
    /// 5 善恶档位的"道义"文学化名。
    /// </summary>
    public static string GetMoralityTierName(MoralityTier tier)
    {
        return tier switch
        {
            MoralityTier.ExtremeEvil => "恶贯满盈",
            MoralityTier.Evil => "浊世染身",
            MoralityTier.Neutral => "常人",
            MoralityTier.Good => "仁善有声",
            MoralityTier.ExtremeGood => "光风霁月",
            _ => "—",
        };
    }
}
