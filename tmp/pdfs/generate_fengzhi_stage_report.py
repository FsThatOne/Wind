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
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
    KeepTogether,
)


OUT = "/Users/bytedance/Desktop/风止_阶段性系统报告.pdf"
FONT = "/System/Library/Fonts/Supplemental/Arial Unicode.ttf"


pdfmetrics.registerFont(TTFont("FengZhi", FONT))


def p(text, style):
    return Paragraph(text, style)


def clean(text):
    return text.replace("--", "-")


styles = getSampleStyleSheet()
styles.add(ParagraphStyle(
    name="CNTitle",
    parent=styles["Title"],
    fontName="FengZhi",
    fontSize=24,
    leading=32,
    alignment=TA_CENTER,
    textColor=colors.HexColor("#222222"),
    spaceAfter=8,
))
styles.add(ParagraphStyle(
    name="CNSubTitle",
    parent=styles["Normal"],
    fontName="FengZhi",
    fontSize=11,
    leading=17,
    alignment=TA_CENTER,
    textColor=colors.HexColor("#666666"),
    spaceAfter=18,
))
styles.add(ParagraphStyle(
    name="CNH1",
    parent=styles["Heading1"],
    fontName="FengZhi",
    fontSize=16,
    leading=23,
    textColor=colors.HexColor("#1f1f1f"),
    spaceBefore=14,
    spaceAfter=8,
))
styles.add(ParagraphStyle(
    name="CNH2",
    parent=styles["Heading2"],
    fontName="FengZhi",
    fontSize=13,
    leading=19,
    textColor=colors.HexColor("#303030"),
    spaceBefore=10,
    spaceAfter=5,
))
styles.add(ParagraphStyle(
    name="CNBody",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=10.4,
    leading=17,
    textColor=colors.HexColor("#292929"),
    alignment=TA_LEFT,
    spaceAfter=5,
))
styles.add(ParagraphStyle(
    name="CNNote",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=9.4,
    leading=15,
    textColor=colors.HexColor("#5c5c5c"),
    spaceAfter=5,
))
styles.add(ParagraphStyle(
    name="CNTiny",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=8.4,
    leading=12,
    textColor=colors.HexColor("#555555"),
))
styles.add(ParagraphStyle(
    name="TableHeader",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=8.6,
    leading=11,
    textColor=colors.white,
    alignment=TA_CENTER,
))
styles.add(ParagraphStyle(
    name="TableCell",
    parent=styles["BodyText"],
    fontName="FengZhi",
    fontSize=8.2,
    leading=11.5,
    textColor=colors.HexColor("#222222"),
))


def bullet(text):
    return p("· " + clean(text), styles["CNBody"])


def section(title):
    return p(title, styles["CNH1"])


def subsection(title):
    return p(title, styles["CNH2"])


def table(data, widths):
    converted = []
    for r, row in enumerate(data):
        style = styles["TableHeader"] if r == 0 else styles["TableCell"]
        converted.append([p(clean(str(c)), style) for c in row])
    t = Table(converted, colWidths=widths, repeatRows=1, hAlign="LEFT")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#343434")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, -1), "FengZhi"),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#c8c8c8")),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f7f7f7")]),
    ]))
    return t


systems_progress = [
    ["维度", "当前覆盖", "阶段判断"],
    ["MVP", "8 个系统均已有设计文档；战斗、武学、心境、对话、存档、战斗 UI 等核心链条成型。", "已从概念期进入可整合评审阶段。"],
    ["Vertical Slice", "7 个系统均已有文档，包括主线、NPC 状态、自然日、地图、感情、朦胧化 UI、物品。", "江南章完整体验的方案骨架已具备。"],
    ["Alpha", "活江湖、顿悟、误会、探索、CG/演出、音乐音效尚未形成独立 GDD。", "仍处于概念占位，需等基础链条稳定后展开。"],
    ["Full Vision", "教学、设置、成就/平台集成尚未开始。", "适合后置，不影响当前阶段主线判断。"],
]


system_dimensions = [
    ["Systems Index 维度", "主要思路", "当前方案状态"],
    ["Core", "把角色属性、NPC 状态、地图场景和存档视为世界事实的地基，让战斗、叙事、关系与时间都能共享同一组事实。", "基础文档已齐；下一步重点是统一跨文档口径。"],
    ["Gameplay", "用 Burst+Read 战斗制造“一读定生死”的短促张力；用武学组合、心境、自然日和未来活江湖构成长期选择重量。", "核心体验已验证并文档化；部分经济与节奏方案仍需收束。"],
    ["Narrative", "线性骨架加呼吸期，保证主线清晰，同时让玩家在章节内选择追查、错过、通信或重逢。", "主线和对话体系已成型；误会系统仍待独立设计。"],
    ["Economy", "物品不是刷数值的仓库，而是江湖旅途的物证、准备与取舍。", "已有方案，但当前复杂度偏高，需要更贴合武侠叙事地减法。"],
    ["Persistence", "存档承载“每个选择都有重量”，记录做过与没做过的事，而不是单纯记录位置和属性。", "框架思路明确；需与新设计系统同步更新。"],
    ["UI", "战斗内清晰如棋局，战斗外朦胧如江湖；信息透明度根据场景服务不同体验。", "战斗 UI 与朦胧化 UI 方向明确，但若上游定义不统一会受影响。"],
    ["Audio / Meta", "音乐音效、教学、设置、成就作为体验抛光和进入门槛控制，不抢当前主线。", "尚未启动，合理后置。"],
]


key_schemes = [
    ["横向方案", "报告级概括"],
    ["核心循环", "紧张的战斗与抉择，接呼吸期探索与关系，再回到心境演变和江湖反馈。"],
    ["玩家选择", "选择不只改变剧情分支，也改变别人如何记住你、哪些内容被错过、哪些信件在之后抵达。"],
    ["活江湖雏形", "NPC 不等玩家原地刷新，而通过远行、代办、飞书、传闻和重逢维持独立人生。"],
    ["情感模型", "感情线不是陪伴条，而是彗星式相遇：离场期间仍有暗号、书信和传闻，重逢因此有温度。"],
    ["信息表达", "同一套底层状态，在战斗中清晰呈现，在江湖中转译为文学、色调、语气和环境线索。"],
    ["内容控制", "用章节线性骨架和少量高质量关系线，抵抗开放世界和浅层内容膨胀。"],
]


risks = [
    ["风险主题", "阶段性判断", "建议处理方向"],
    ["跨文档一致性", "季节天数、境界映射、战斗行动、依赖编号等仍有冲突。", "先做一次系统事实归一，再进入更大规模实现。"],
    ["认知负荷", "当前已有多条并行成长与追踪线，可能压过叙事优先目标。", "把 MVP/VS 只保留玩家当下必须理解的 7-8 个信号。"],
    ["战斗主导策略", "“看懂后反制”若过强，会让 Burst+Read 失去博弈。", "通过敌方行为、成本、节奏和 Boss 模式维持可读但不可背板。"],
    ["物品系统风格", "装备品级、品质、词缀有回到 ARPG 模板的风险。", "把装备收束为叙事锚点和少量明确取舍，弱化刷词条感。"],
    ["时间经济", "自然日、支线、关系、修炼和主线限时会竞争玩家注意力。", "建立优先级提示和保护机制，避免玩家被系统推着焦虑。"],
]


next_focus = [
    "先修正全 GDD 一致性中的关键事实：时间、境界、战斗行动、存档心境精度与依赖关系。",
    "把 MVP 范围重新压实：起点到江南前半，确保一场战斗、一次心境选择、一段飞书/暗号关系、一次可感知的时间推进都能闭环。",
    "对物品与时间经济做减法审查，确保它们服务武侠叙事和路线抉择，而不是制造后台管理压力。",
    "在 Alpha 系统展开前，优先补齐活江湖层、误会系统、探索/洞察的“方案级边界”，避免它们反向扩大主线内容量。",
    "后续报告建议以 Vertical Slice Readiness 为目标，检查江南章是否已能支撑 6-8 小时完整体验。",
]


story = []
story.append(p("《风止》阶段性系统报告", styles["CNTitle"]))
story.append(p("基于 Systems Index 与现有 GDD 文档整理 · 2026-06-05", styles["CNSubTitle"]))

story.append(section("一、报告定位"))
story.append(p(clean("本报告面向阶段性复盘，不替代任何单系统 GDD。它只概括当前文档中已经形成的思路、系统维度和主要方案，不展开代码实现、接口定义、公式细节或具体设计参数。"), styles["CNBody"]))
story.append(p(clean("当前项目已经从“概念确立”推进到“系统 GDD 成批成型”的阶段。Systems Index 识别了 24 个系统，MVP 与 Vertical Slice 所需的 15 个系统文档均已存在；Alpha 与 Full Vision 系统仍处于待设计状态。"), styles["CNBody"]))

story.append(section("二、总体判断"))
story.append(bullet("《风止》的核心定位清晰：2D 像素武侠叙事 RPG，以复仇追查、心境演变、忠贞感情和 Burst+Read 战斗形成差异化。"))
story.append(bullet("项目支柱明确：江湖是活的、选择有重量、武侠味先于游戏味、情感深度优先于内容广度。现有系统方案基本围绕这四点展开。"))
story.append(bullet("MVP 与 Vertical Slice 的设计覆盖率高，但一致性和范围控制已经成为下一阶段最大风险。现在更需要收束，而不是继续加系统。"))
story.append(bullet("报告建议把下一阶段定义为“Vertical Slice Foundation Consolidation”：先统一事实、压实核心闭环，再展开 Alpha 系统。"))

story.append(section("三、Systems Index 阶段进展"))
story.append(table(systems_progress, [33*mm, 100*mm, 42*mm]))
story.append(Spacer(1, 8))
story.append(p(clean("注：Systems Index 原文的进度统计仍保留较早状态；本表按当前目录中实际存在的 GDD 文档归纳，不修改原文档。"), styles["CNNote"]))

story.append(section("四、按系统维度归纳"))
story.append(table(system_dimensions, [30*mm, 88*mm, 57*mm]))

story.append(section("五、已形成的主要方案"))
story.append(table(key_schemes, [38*mm, 137*mm]))

story.append(section("六、阶段性风险"))
story.append(p(clean("6 月 5 日跨 GDD 审查已经指出多项一致性和设计理论问题。报告级结论是：这些问题并非说明方向错误，而是说明系统文档已经足够多，到了必须统一事实和收束范围的阶段。"), styles["CNBody"]))
story.append(table(risks, [38*mm, 78*mm, 59*mm]))

story.append(section("七、下一阶段建议"))
for item in next_focus:
    story.append(bullet(item))

story.append(section("八、阶段结论"))
story.append(p(clean("《风止》目前最强的部分，是“叙事幻想”和“系统表达”之间已经建立了同一个方向：战斗负责清明，江湖负责朦胧；主线给出复仇与真相，心境和关系决定玩家以什么身份走到终幕。"), styles["CNBody"]))
story.append(p(clean("下一阶段不宜追求更多系统数量，而应优先让已成型的 MVP/Vertical Slice 系统彼此说同一种语言。只要事实归一、范围收束、玩家认知负荷可控，江南章就具备进入 Vertical Slice 打磨的基础。"), styles["CNBody"]))


class NumberedCanvasDoc(BaseDocTemplate):
    pass


def header_footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("FengZhi", 8)
    canvas.setFillColor(colors.HexColor("#666666"))
    canvas.drawString(20 * mm, 286 * mm, "《风止》阶段性系统报告")
    canvas.setStrokeColor(colors.HexColor("#d6d6d6"))
    canvas.line(20 * mm, 281 * mm, 190 * mm, 281 * mm)
    canvas.drawRightString(190 * mm, 10 * mm, str(doc.page))
    canvas.restoreState()


doc = NumberedCanvasDoc(
    OUT,
    pagesize=A4,
    rightMargin=20 * mm,
    leftMargin=20 * mm,
    topMargin=26 * mm,
    bottomMargin=18 * mm,
    title="《风止》阶段性系统报告",
    author="Codex",
)
frame = Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="normal")
doc.addPageTemplates([PageTemplate(id="main", frames=[frame], onPage=header_footer)])
doc.build(story)
print(OUT)
