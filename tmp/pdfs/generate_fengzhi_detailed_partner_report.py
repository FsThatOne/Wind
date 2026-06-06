from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)


OUT = "/Users/bytedance/Desktop/风止_阶段性系统详版报告.pdf"
FONT = "/System/Library/Fonts/Supplemental/Arial Unicode.ttf"


pdfmetrics.registerFont(TTFont("FengZhi", FONT))


def clean(text):
    return (
        str(text)
        .replace("--", "-")
        .replace("–", "-")
        .replace("—", "-")
        .replace("×", "x")
    )


styles = getSampleStyleSheet()
styles.add(ParagraphStyle(
    name="CNTitle",
    parent=styles["Title"],
    fontName="FengZhi",
    fontSize=24,
    leading=32,
    alignment=TA_CENTER,
    textColor=colors.HexColor("#202020"),
    spaceAfter=8,
))
styles.add(ParagraphStyle(
    name="CNSubTitle",
    parent=styles["Normal"],
    fontName="FengZhi",
    fontSize=10.5,
    leading=16,
    alignment=TA_CENTER,
    textColor=colors.HexColor("#666666"),
    spaceAfter=16,
))
styles.add(ParagraphStyle(
    name="CNH1",
    parent=styles["Heading1"],
    fontName="FengZhi",
    fontSize=15.5,
    leading=22,
    textColor=colors.HexColor("#1d1d1d"),
    spaceBefore=12,
    spaceAfter=7,
))
styles.add(ParagraphStyle(
    name="CNH2",
    parent=styles["Heading2"],
    fontName="FengZhi",
    fontSize=12.2,
    leading=17,
    textColor=colors.HexColor("#303030"),
    spaceBefore=7,
    spaceAfter=4,
))
styles.add(ParagraphStyle(
    name="CNBody",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=9.5,
    leading=15.6,
    alignment=TA_LEFT,
    textColor=colors.HexColor("#282828"),
    spaceAfter=4.2,
))
styles.add(ParagraphStyle(
    name="CNNote",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=8.8,
    leading=13.6,
    textColor=colors.HexColor("#555555"),
    spaceAfter=4,
))
styles.add(ParagraphStyle(
    name="TableHeader",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=7.9,
    leading=10.4,
    textColor=colors.white,
    alignment=TA_CENTER,
))
styles.add(ParagraphStyle(
    name="TableCell",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=7.6,
    leading=10.6,
    textColor=colors.HexColor("#242424"),
    alignment=TA_LEFT,
))


def p(text, style="CNBody"):
    return Paragraph(clean(text), styles[style])


def h1(text):
    return p(text, "CNH1")


def h2(text):
    return p(text, "CNH2")


def bullet(text):
    return p("· " + clean(text), "CNBody")


def note(text):
    return p(text, "CNNote")


def table(data, widths, header_color="#343434", font_size=None):
    converted = []
    for row_index, row in enumerate(data):
        style = styles["TableHeader"] if row_index == 0 else styles["TableCell"]
        converted.append([Paragraph(clean(cell), style) for cell in row])
    t = Table(converted, colWidths=widths, repeatRows=1, hAlign="LEFT")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor(header_color)),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, -1), "FengZhi"),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#c7c7c7")),
        ("LEFTPADDING", (0, 0), (-1, -1), 4.2),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4.2),
        ("TOPPADDING", (0, 0), (-1, -1), 4.5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4.5),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f7f7f7")]),
    ]))
    if font_size:
        t.setStyle(TableStyle([
            ("FONTSIZE", (0, 0), (-1, -1), font_size),
        ]))
    return t


def footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("FengZhi", 8)
    canvas.setFillColor(colors.HexColor("#777777"))
    canvas.drawString(18 * mm, 11 * mm, "《风止》阶段性系统详版报告")
    canvas.drawRightString(192 * mm, 11 * mm, f"{doc.page}")
    canvas.restoreState()


doc = BaseDocTemplate(
    OUT,
    pagesize=A4,
    leftMargin=17 * mm,
    rightMargin=17 * mm,
    topMargin=16 * mm,
    bottomMargin=18 * mm,
)
frame = Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="normal")
doc.addPageTemplates([PageTemplate(id="report", frames=[frame], onPage=footer)])


story = []

story.append(p("《风止》阶段性系统详版报告", "CNTitle"))
story.append(p("面向合作伙伴的系统方案说明 · 2026-06-05", "CNSubTitle"))
story.append(p("本报告基于当前 Systems Index 与现有 GDD 文档整理，目标是让合作伙伴理解《风止》几套核心系统的设计意图、主要规则、判定方式与跨系统联动。报告不展开代码实现、接口细节和工程设计，只保留产品方案与落地逻辑。", "CNBody"))
story.append(p("一句话判断：项目的核心幻想已经成型，下一阶段重点不是继续增加系统，而是把心境、情感、战斗、地图、物品这几套会直接影响玩家体验的系统规则统一、压实，并用 Vertical Slice 验证它们是否能在江南章形成 6-8 小时的完整体验。", "CNBody"))

story.append(h1("一、项目总览"))
story.append(bullet("定位：2D 像素武侠叙事 RPG，核心体验是复仇追查、心境演变、忠贞情感和短促高压的读招战斗。"))
story.append(bullet("设计支柱：江湖是活的、选择有重量、武侠味先于游戏味、情感深度优先于内容广度。"))
story.append(bullet("Systems Index 当前识别 24 个系统；MVP 与 Vertical Slice 的关键系统已基本有 GDD 支撑，Alpha/Full Vision 系统仍适合后置。"))
story.append(bullet("面向合作伙伴可强调：本项目不是开放世界堆量，也不是刷装备 ARPG，而是以少量高密度系统承载“我这一路怎样成为这个人”的叙事回望。"))

story.append(table([
    ["维度", "当前阶段", "合作伙伴需要理解的重点"],
    ["MVP 核心", "角色属性、战斗、武学、对话、心境、存档、战斗 UI 等已形成方案骨架。", "能支撑最小可玩闭环：选择 - 战斗 - 反馈 - 存档 - 再选择。"],
    ["Vertical Slice", "主线、NPC 状态、自然日、地图、感情、朦胧化 UI、物品均已有设计。", "江南章可作为第一段完整体验验证区。"],
    ["Alpha 后置", "活江湖、误会、探索洞察、演出、音乐音效等仍待独立展开。", "不宜过早膨胀，应等核心闭环稳定后再接入。"],
], [26 * mm, 56 * mm, 88 * mm]))

story.append(h1("二、心境系统：双轴 + 善恶如何决定人生终点"))
story.append(p("心境系统是《风止》的“命运坐标”。它不改变攻击、防御等战斗数值，而是记录玩家在对话、行为和少数心境战斗中的长期倾向。玩家不直接看到数值，只通过朦胧化 UI、NPC 语气、奇遇条件和最终结局感受到自己的变化。", "CNBody"))

story.append(h2("1. 三条轴各自负责什么"))
story.append(table([
    ["轴", "范围与初值", "含义", "玩家感知"],
    ["执念 ↔ 释怀", "-50 到 +50，初始 -5", "面对师门血仇时，是越陷越深，还是学会放下。", "通过复仇相关选项、仇人处置、终幕态度体现。"],
    ["入世 ↔ 出世", "-50 到 +50，初始 0", "面对江湖与庙堂时，是投身纷争，还是远离是非。", "通过是否介入他人命运、是否承担江湖责任体现。"],
    ["善 ↔ 恶", "-50 到 +50，初始 0，隐藏轴", "玩家行为的道德后果，允许堕落，也允许回头。", "不显示数值，通过 NPC 畏惧/敬重、传闻和结局色调体现。"],
], [32 * mm, 34 * mm, 64 * mm, 40 * mm]))

story.append(h2("2. 心境怎样变化"))
story.append(bullet("日常位移：支线对话、回信、小抉择通常带来 +/-1 到 +/-3 的变化，单次不显眼，但会累积出性格。"))
story.append(bullet("关键位移：主线节点、生死抉择、仇人对峙通常带来 +/-5 到 +/-10 的变化，直接塑造路线倾向。"))
story.append(bullet("心境战斗：少数战斗胜负本身带有叙事意义，例如复仇战胜利可能加深执念，保护弱者失败可能带来善恶变化。"))
story.append(bullet("善恶轴更密集且可逆：日常行为、支线结果、主线决策、转念事件都可能影响善恶，支持“堕入深渊”和“浪子回头”。"))

story.append(h2("3. 双轴区域与结局基础方向"))
story.append(table([
    ["区域判定", "入世 <= -15", "中立 -14 到 +14", "出世 >= +15"],
    ["执念 <= -15", "孤剑入世", "执念未定", "风止尘湮"],
    ["中立 -14 到 +14", "入世未定", "中庸", "出世未定"],
    ["释怀 >= +15", "白衣入世", "释怀未定", "大隐于市"],
], [35 * mm, 55 * mm, 55 * mm, 55 * mm]))

story.append(table([
    ["基础结局", "触发倾向", "核心含义", "善恶如何修饰"],
    ["白衣行天下", "释怀 + 入世", "放下私仇后仍入江湖，以剑行侠。", "善则侠名远播；恶则名为行侠、实则令人畏惧。"],
    ["孤剑斩世", "执念 + 入世", "不放下仇怨，继续以孤剑对抗江湖。", "善则不伤无辜；恶则血杀成名。"],
    ["大隐于市", "释怀 + 出世", "放下仇怨，归于平凡或山林。", "善则余泽仍在；恶则隐居更像逃避。"],
    ["风止剑鸣", "执念 + 出世", "守着师门旧梦，与世界渐远。", "善则化仇为守护；恶则被恨意吞噬。"],
    ["未定之人", "双轴长期未形成明确倾向", "玩家始终没有作出真正的人生选择。", "仍会被善恶色调修饰。"],
], [28 * mm, 34 * mm, 66 * mm, 42 * mm]))

story.append(note("当前需统一的产品口径：心境文档将极恶写作“基础结局的极恶色调变体”，感情文档写作“morality <= -30 时触发第六魔道结局 override”。建议下一轮先决定：魔道是独立第六结局，还是极恶色调的一类特殊变体。"))

story.append(h1("三、情感系统：彗星模型、态度与里程碑"))
story.append(p("情感系统的目标不是做“好感度攻略”，而是让四位女主像彗星一样拥有自己的轨迹：她们会出现、离开、来信、留下暗号、被传闻提起，再在某些节点与主角重逢。玩家感受到的不是“她一直跟队”，而是“她也在江湖中走自己的路”。", "CNBody"))

story.append(h2("1. 双层结构：即时态度 + 不可逆事实"))
story.append(table([
    ["层", "作用", "是否可逆", "由什么影响"],
    ["态度档位", "表示 NPC 当前对主角的情绪温度，决定对话语气、是否求援、是否愿意推进关系。", "可上下波动", "基础关系、心境兼容、善恶反应、误会事件。"],
    ["关系里程碑", "记录已经发生的叙事事实，例如相识、共患难、交托心事。", "通常不可逆", "由专属剧情节点触发，并给态度设置地板。"],
], [27 * mm, 78 * mm, 30 * mm, 35 * mm]))

story.append(table([
    ["分数", "态度档位", "常见表现"],
    ["+3", "生死相托", "可托付性命，结缘后稳定停留在此档。"],
    ["+2", "推心置腹", "能谈隐秘心事，是重要关系节点门槛。"],
    ["+1", "以礼相待", "亲近或敬重，但仍守江湖分寸。"],
    ["0", "萍水相逢", "认识但不亲密。"],
    ["-1", "心有疑云", "开始怀疑主角动机。"],
    ["-2", "避而不见", "回避接触或减少回应。"],
    ["-3", "闻名生惧", "因主角名声或行为而畏惧。"],
    ["-4", "拔剑相向", "不可修复或接近不可修复的对立。"],
], [20 * mm, 34 * mm, 116 * mm]))

story.append(h2("2. 关系如何判断能不能继续推进"))
story.append(table([
    ["里程碑", "含义", "态度地板", "推进门槛"],
    ["相识", "不再是陌路人，有正式交谈。", "萍水相逢 0", "自动或初遇剧情。"],
    ["信任建立", "经历事件后选择信任。", "以礼相待 +1", "态度至少 +1。"],
    ["共渡险境", "在生死或高压情境中互相依靠。", "推心置腹 +2", "态度至少 +1。"],
    ["交托心事", "一方主动坦露隐秘。", "推心置腹 +2", "态度至少 +2。"],
    ["结缘", "双方作出明确承诺。", "生死相托 +3", "已交托心事、态度至少 +2、且未结缘他人。"],
    ["诀别", "不可修复的决裂。", "拔剑相向 -4", "极端剧情强制覆写。"],
], [24 * mm, 70 * mm, 35 * mm, 41 * mm]))

story.append(h2("3. 彗星存在感如何维持"))
story.append(table([
    ["机制", "玩家怎样遇见", "频率/特征", "情感作用"],
    ["暗号", "在场景中发现她留下的标记。", "每章 1-2 次，偏主动发现。", "让玩家意识到她走过这里，通常微量增进态度。"],
    ["书信", "通过飞书或信使系统送达。", "受自然日和 NPC 状态驱动。", "传递情报、心事或牵挂，支撑离场期间的关系。"],
    ["传闻", "在客栈、路人、江湖消息中听见。", "可错过，不影响主线。", "证明女主也在独立行动。"],
    ["偶遇", "在预埋叙事节点或呼吸期短暂重逢。", "不是随机刷出，而是内容设计安排。", "推进里程碑，制造“缘分到了”的感觉。"],
], [22 * mm, 49 * mm, 44 * mm, 55 * mm]))

story.append(note("结缘不是单纯好感满即可触发。必须满足：已交托心事、当前态度达到推心置腹、尚未结缘他人、进入专属结缘节点。结缘后还要通过心境兼容性决定终幕是同行还是道别。"))

story.append(h1("四、战斗系统：Burst+Read 与刚柔巧"))
story.append(p("战斗系统追求的是武侠对决的“读招”和“破局”，而不是长时间消耗战。每回合敌人亮出下一招的体系类型，但不公开具体招式。玩家根据刚、柔、巧的克制关系选择出招、反制、调息、心法或道具，在 5-15 回合内寻找一击决胜窗口。", "CNBody"))

story.append(h2("1. 战斗核心循环"))
story.append(table([
    ["阶段", "发生什么", "设计目的"],
    ["回合开始", "恢复少量内息，破绽自然衰减。", "让节奏有呼吸，避免破绽永久滚雪球。"],
    ["读意", "敌人显示下一招体系：刚 / 柔 / 巧。", "给玩家可读信息，但不暴露全部答案。"],
    ["抉择", "玩家选择出招、反制、调息、心法/特殊招式或使用道具。", "在克制、资源、风险之间做选择。"],
    ["结算", "双方同时结算伤害、内息、破绽与特殊效果。", "保留高手对决的并行感。"],
    ["爆发", "目标破绽达到阈值后可发动一击决胜。", "制造战斗高潮，但不是无条件秒杀。"],
], [25 * mm, 82 * mm, 63 * mm]))

story.append(h2("2. 刚柔巧怎样互相作用"))
story.append(table([
    ["关系", "克制结果", "被克结果", "同系结果"],
    ["刚克巧", "刚方伤害提高，巧方破绽增加。", "巧方伤害降低，自己反受节奏压力。", "正常伤害，不额外改变破绽。"],
    ["巧克柔", "巧方以变化破柔，增加对手破绽。", "柔方被牵制，难以发挥卸力。", "正常伤害，不额外改变破绽。"],
    ["柔克刚", "柔方以卸力化刚，反让刚方露出破绽。", "刚方强攻被化，收益下降。", "正常伤害，不额外改变破绽。"],
], [26 * mm, 60 * mm, 58 * mm, 26 * mm]))

story.append(bullet("克制：伤害约 1.3 倍，被克制者增加更多破绽。"))
story.append(bullet("被克：伤害约 0.7 倍，主动方也可能因判断失误留下破绽。"))
story.append(bullet("反制：需要消耗内息，并要求玩家选择克制敌人已亮出的体系；成功会额外制造破绽。"))
story.append(bullet("一击决胜：当对方破绽达到门槛后开放，造成高倍率伤害和专属演出，但设计上不保证必杀。"))
story.append(bullet("道具：使用战斗道具消耗本回合行动，适合救急或提前铺垫，而不是无成本补强。"))

story.append(h2("3. 战斗与其他系统的联动"))
story.append(table([
    ["联动对象", "怎样影响战斗"],
    ["角色属性", "提供气血、攻击、防御、速度、内息回复、破绽等基础数值。"],
    ["武学系统", "提供已装备招式列表；玩家理论上有 6 个基础招式槽和心法专属招式。"],
    ["物品系统", "提供战斗可用消耗品；装备在战前影响属性，战斗中不能临时更换。"],
    ["心境系统", "少数心境战斗的结果会影响心境，但心境本身不加战斗数值。"],
], [35 * mm, 135 * mm]))

story.append(h1("五、地图系统：线性骨架中的活江湖空间"))
story.append(p("地图/场景系统负责让玩家相信江湖有空间、有时节、有路途。它不做完全开放世界，而采用“主线线性推进 + 章节呼吸期开放”的结构：主线保证方向，呼吸期让玩家在当前章节的已解锁区域中选择去哪里、见谁、查哪条线。", "CNBody"))

story.append(h2("1. 两层空间：地点与场景"))
story.append(table([
    ["层级", "定义", "例子", "规则"],
    ["地点 Location", "大地图上的标记点，是玩家远行和解锁的单位。", "柳家镇、江南水乡、塞北据点等。", "每个地点包含一个入口场景和若干内部场景。"],
    ["场景 Scene", "玩家实际活动、对话、战斗和探索的空间。", "镇口、集市、客栈、镇北树林。", "同地点内可直连；跨地点必须回到大地图。"],
], [29 * mm, 55 * mm, 43 * mm, 43 * mm]))

story.append(h2("2. 解锁、移动与呼吸期"))
story.append(table([
    ["机制", "规则", "玩家体验"],
    ["访问状态", "locked 不显示；known 灰色可见但不可进入；unlocked 可前往。", "世界不是一次性摊开，而是随主线逐渐变大。"],
    ["连接图", "场景内部连接与 exit_to_map 分开配置，可支持密道、单向门、剧情封锁。", "移动像真实地点，而不是菜单传送。"],
    ["呼吸期", "主线节点完成后，当前章节已解锁地点可自由往返。", "玩家有时间处理关系、支线、装备、信件和传闻。"],
    ["大地图旅行", "移动消耗时间和体力，格子路程可能触发隐藏遭遇。", "赶路本身有代价和偶遇感。"],
    ["驿站快行", "只在已解锁站点之间提供快速移动。", "降低重复跑路，但不破坏首次探索重量。"],
], [28 * mm, 86 * mm, 56 * mm]))

story.append(h2("3. 时间、季节与天气怎样参与地图"))
story.append(bullet("自然日系统掌管时辰与四季，地图系统订阅变化并切换场景光照、色调和环境表现。"))
story.append(bullet("四季为春夏秋冬，每季 30 个游戏日；天气每日刷新，包含晴、多云、小雨、大雨、雪、雾、风沙等状态。"))
story.append(bullet("场景可设置季节敏感度：例如江南春雨、塞北风雪、秋日客栈传闻等，让同一地点在不同时间有不同气质。"))
story.append(bullet("章节基调与季节不是同一件事：主线可以决定章节情绪，季节/天气提供世界自然变化，终幕还可能由心境进一步改变色调。"))

story.append(h1("六、物品与装备：分级、品质与词条"))
story.append(p("物品系统承担两层功能：一是提供战斗和探索准备，二是把旅途中的获得物变成有来历的江湖物证。当前方案里，背包不制造槽位焦虑，但装备、丹药、材料、关键物品会通过品级、品质、词条、可交易性和使用场景形成取舍。", "CNBody"))

story.append(h2("1. 物品大类"))
story.append(table([
    ["类别", "战斗中可用", "可堆叠", "可交易", "说明"],
    ["战斗消耗品", "可用", "可堆叠", "可交易", "伤药、临时增益等；使用会消耗本回合行动。"],
    ["探索消耗品", "不可用", "可堆叠", "可交易", "解谜、赶路、采集或场景互动相关。"],
    ["装备", "战前装配", "不可堆叠", "可交易", "武器、护甲、足具、饰品，提供属性与词条。"],
    ["关键物品", "通常不可用", "不可堆叠", "通常不可交易", "书信、信物、配方、剧情凭证等，独立列表管理。"],
], [28 * mm, 25 * mm, 25 * mm, 25 * mm, 67 * mm]))

story.append(h2("2. 装备槽位与品级"))
story.append(table([
    ["槽位", "类型", "主要影响"],
    ["主手", "武器", "攻击、暴击、速度。"],
    ["身甲", "护甲", "防御、气血上限。"],
    ["足具", "鞋", "速度、闪避。"],
    ["饰品 1 / 饰品 2", "饰品", "任意属性，由具体饰品定义；同一件饰品不能占两个槽。"],
], [34 * mm, 36 * mm, 100 * mm]))

story.append(table([
    ["品级", "基础属性系数", "定位", "常见来源"],
    ["九品", "x1.0", "极常见", "初始、商店。"],
    ["八品", "x1.2", "常见", "商店、低级掉落。"],
    ["七品", "x1.5", "普通", "掉落、锻造。"],
    ["六品", "x1.8", "较少", "中级掉落、锻造。"],
    ["五品", "x2.2", "稀有", "高级掉落、锻造。"],
    ["四品", "x2.7", "稀有", "Boss、拍卖。"],
    ["三品", "x3.3", "珍贵", "Boss、拍卖、任务。"],
    ["二品", "x4.0", "极珍贵", "主线、隐藏任务。"],
    ["一品", "x5.0", "极稀有", "终章、隐藏 Boss。"],
    ["传说", "x7.0", "全游戏个位数", "唯一条件获取。"],
], [28 * mm, 34 * mm, 36 * mm, 62 * mm]))

story.append(note("品级是装备的“底子”：同名装备品级固定，不随机；品级越高，基础属性越高。也就是说，一把传说剑的身份不是刷出来的，而是世界观与获取条件定义出来的。"))

story.append(h2("3. 品质与词条怎样判断"))
story.append(table([
    ["品质", "评分区间", "颜色表达", "含义"],
    ["极品", "86%-100%", "金色", "词条数量与数值都接近上限。"],
    ["上品", "71%-85%", "紫色", "优秀，但仍有提升空间。"],
    ["中品", "56%-70%", "蓝色", "可用且稳定。"],
    ["下品", "40%-55%", "绿色", "基础可用；无词条装备默认下品。"],
], [26 * mm, 32 * mm, 32 * mm, 80 * mm]))

story.append(table([
    ["规则", "说明"],
    ["词条池", "装备生成时从对应品级的词条池抽取，避免低品级装备出现不合身份的极强词条。"],
    ["词条数量上限", "九品 1 条；八/七品 2 条；六/五品 3 条；四/三品 4 条；二/一品 5 条；传说 6 条。"],
    ["词条数值", "每条词条在满值 40%-100% 之间浮动，保证不会出现完全废词条。"],
    ["品质评分", "装备品质综合看“词条数量占上限比例”和“词条平均质量”，当前权重为数量 40%、质量 60%。"],
    ["显示方式", "品质主要通过装备名颜色体现，不额外用文字标签堆 UI。"],
], [35 * mm, 135 * mm]))

story.append(h2("4. 制作、交易与经济"))
story.append(bullet("炼丹：草药加配方产出丹药，丹药品质分极/上/中/下，对应药效约 120% / 100% / 80% / 60%。"))
story.append(bullet("锻造：材料、矿石和银两产出装备；品级由配方决定，品质与词条在生成时浮动。"))
story.append(bullet("精炼：消耗材料提升装备基础属性，但每项属性最多接近该品级上限，避免无限成长。"))
story.append(bullet("经济：玩家应常态“小富”但不能暴富，需要在买药、升级装备、竞拍稀有物之间取舍。"))
story.append(note("合作伙伴沟通建议：物品系统当前信息完整，但也有 ARPG 化风险。后续应把随机词条控制在“江湖造化”范围内，而不是让玩家以刷词条为主要目标。"))

story.append(h1("七、核心系统怎样联动成体验"))
story.append(table([
    ["体验链路", "发生了什么", "最终让玩家感到什么"],
    ["地图 -> 时间 -> 情感", "玩家在呼吸期赶路，消耗时辰和体力；路上收到女主飞书或听到传闻。", "她不在队里，但她也活在江湖中。"],
    ["对话 -> 心境 -> NPC 态度", "玩家做出冷酷或仁义选择，心境与善恶变化；NPC 下次对话态度改变。", "选择不是当场结算，而是在之后回声般出现。"],
    ["武学/装备 -> 战斗", "玩家战前配置招式、心法和装备；战斗中根据敌人体系读招。", "胜负来自准备与判断，而不只是数值堆叠。"],
    ["战斗 -> 心境", "少数心境战斗的胜负进入叙事结算，改变执念、释怀或善恶。", "打赢不一定是精神上的胜利，打输也可能改变人。"],
    ["心境 -> 感情结局", "结缘后仍要看终幕心境是否兼容。", "爱情不是攻略奖励，而是两条路能否走到一起。"],
], [35 * mm, 78 * mm, 57 * mm]))

story.append(h1("八、当前需统一与收束的重点"))
story.append(table([
    ["主题", "阶段问题", "建议"],
    ["魔道结局定位", "心境文档与感情文档对极恶/第六结局的定义不同。", "先定产品口径：独立第六结局，或极恶变体。"],
    ["物品复杂度", "品级、品质、词条、精炼、拍卖都已具备，可能偏 ARPG。", "保留身份感和少量取舍，减少刷词条驱动。"],
    ["战斗行动表", "出招、反制、决胜、调息、心法、道具都存在，需要最终行动注册口径。", "以 5-15 回合短战斗为目标做一次战斗闭环验证。"],
    ["时间常量", "自然日与地图季节定义曾出现冲突。", "时间常量由自然日系统统一拥有，地图只消费。"],
    ["心境存储精度", "善恶若只存档位，会影响极恶/魔道等结局判断。", "保留连续值，用档位只做展示和条件查询。"],
    ["合作伙伴展示", "系统很多，但卖点不是“多”，而是少量系统互相回声。", "Demo/VS 应优先展示一次选择如何跨系统延迟反馈。"],
], [32 * mm, 76 * mm, 62 * mm]))

story.append(h1("九、阶段性结论"))
story.append(p("《风止》目前已经具备清晰的系统方向：用心境决定人生归处，用彗星模型承载江湖爱情，用 Burst+Read 战斗承载武侠对决，用地图和自然日制造活江湖的空间与时间，用物品系统提供旅途准备和物证感。", "CNBody"))
story.append(p("对合作伙伴而言，这一阶段最值得关注的不是系统数量，而是这些系统已经能彼此解释：为什么一封信会在某天抵达，为什么某个 NPC 从敬重变为疏远，为什么一场战斗胜利反而让主角更接近深渊，为什么结缘之后仍可能道别。只要 Vertical Slice 能把这些“后果的回声”做出来，《风止》的差异化就会很明确。", "CNBody"))
story.append(p("建议下一阶段以“江南章完整闭环”为验证目标：固定一段主线、两到三场关键战斗、一位女主彗星轨迹、一组心境分歧、一次地图呼吸期和一套基础装备循环，先证明玩家能在 6-8 小时内真实感到“这一路是我走出来的”。", "CNBody"))

story.append(h1("十、合作伙伴可带走的判断"))
story.append(table([
    ["问题", "回答"],
    ["这是什么类型的项目？", "不是开放世界堆量，也不是刷装备 ARPG，而是高密度武侠叙事 RPG。"],
    ["最核心的差异化是什么？", "选择会跨系统延迟反馈：心境、NPC 态度、情感终点、地图事件和结局互相回声。"],
    ["下一阶段最该验证什么？", "江南章 Vertical Slice 是否能让玩家在 6-8 小时内感到选择有重量、江湖在流动、情感有牵挂。"],
    ["当前最大风险是什么？", "系统复杂度压过叙事；尤其是物品词条、时间经济和结局口径需要收束统一。"],
], [45 * mm, 125 * mm]))


doc.build(story)
print(OUT)
