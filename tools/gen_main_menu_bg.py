"""
《风止》主菜单水墨背景生成器 v5（最终版）
关键改进：
- 剑客：简化为大写意白色剪影，广袖融入袍身，不再有"机械臂"感
- 山峰：在中点位移基础上手动添加几座尖峰，更接近中国山水的奇崛感
- 山门：放置在最高的中景山峰顶部，确保可见
- 朱砂烟：更明显、更长的一丝烟
"""
from PIL import Image, ImageDraw, ImageFilter
import random
import math
import os

random.seed(2026)

W, H = 1920, 1080
INK      = (26, 26, 46)
INK_DEEP = (12, 12, 26)
PAPER    = (245, 240, 232)
CINNABAR = (199, 62, 62)
GOLD     = (212, 168, 67)
MIST     = (180, 185, 200)
SKY_TOP  = (10, 10, 22)

OUTPUT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "feng-zhi", "assets", "ui", "main_menu", "main_menu_bg_ink.png"
)


def lerp(a, b, t):
    return a + (b - a) * t


def lerp_color(c1, c2, t):
    return (int(lerp(c1[0], c2[0], t)),
            int(lerp(c1[1], c2[1], t)),
            int(lerp(c1[2], c2[2], t)))


def midpoint_displacement(width, horizon_y, max_peak_height, roughness=0.55,
                          seed=0, iterations=9, valley_depth=0.15):
    rng = random.Random(seed)
    n = 2 ** iterations + 1
    values = [rng.uniform(0, max_peak_height * 0.15),
              rng.uniform(0, max_peak_height * 0.15)]
    for level in range(iterations):
        new_vals = []
        displace = max_peak_height * (roughness ** (level + 1))
        for i in range(len(values) - 1):
            mid = (values[i] + values[i+1]) / 2 + rng.uniform(-displace, displace)
            mid = max(-valley_depth * max_peak_height, mid)
            new_vals.append(values[i])
            new_vals.append(mid)
        new_vals.append(values[-1])
        values = new_vals
    result = []
    for x in range(width):
        t = x / (width - 1) * (len(values) - 1)
        i = int(t); frac = t - i
        i = min(i, len(values) - 2)
        v = values[i] * (1-frac) + values[i+1] * frac
        result.append(int(horizon_y - v))
    return result


def add_sharp_peak(height_map, center_x, peak_height, base_width, pointiness=0.7):
    """在高度数组上叠加一座尖峰（中国山水的奇峰）。"""
    for x in range(len(height_map)):
        dist = abs(x - center_x)
        if dist < base_width:
            t = dist / base_width
            # 尖峰曲线：抛物线+尖顶
            h = peak_height * math.pow(1 - t, 1 + pointiness * 2)
            new_y = height_map[x] - int(h)
            height_map[x] = min(height_map[x], new_y)


def draw_mountains(img, height_map, color, alpha, blur=0):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    pts = [(x, height_map[x]) for x in range(W)]
    poly = [(0, H)] + pts + [(W-1, H)]
    d.polygon(poly, fill=(*color, alpha))
    if blur > 0:
        ov = ov.filter(ImageFilter.GaussianBlur(radius=blur))
    return Image.alpha_composite(img.convert('RGBA'), ov)


def mist_band(img, y, thickness, intensity=0.4, blur=30, color=MIST):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    for yy in range(max(0, y-thickness), min(H, y+thickness)):
        dist = abs(yy - y) / thickness
        a = int(intensity * 240 * math.exp(-(dist**2)*4.5))
        if a > 0:
            d.line([(0,yy),(W,yy)], fill=(*color, a))
    ov = ov.filter(ImageFilter.GaussianBlur(radius=blur))
    return Image.alpha_composite(img.convert('RGBA'), ov)


# ---------------------------------------------------------------------------
# 剑客 —— 大写意白色剪影
# ---------------------------------------------------------------------------
def draw_swordsman(img, cx, feet_y, s=1.0):
    """
    大写意剑客背影：整体为临风而立的白色钟形轮廓，
    参考梁楷减笔画和水墨山水中的点景人物。
    不单独画手臂/袖子，而是将广袖融入袍身的大形。
    动势主要由：下摆被风推向右、白色发带高飘、袍身曲线传达。
    """
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)

    head_r = int(8*s)
    total_h = int(140*s)
    neck_y = feet_y - total_h
    head_cy = neck_y - int(2*s)
    hem_w = int(52*s)        # 下摆宽
    shoulder_w = int(14*s)   # 肩宽（窄，因为是背影）
    wind = int(14*s)         # 风吹偏移量

    # 投影
    sh = Image.new('RGBA', img.size, (0,0,0,0))
    ImageDraw.Draw(sh).ellipse(
        [cx-int(45*s), feet_y-int(4*s), cx+int(45*s), feet_y+int(5*s)],
        fill=(0,0,0,28))
    img = Image.alpha_composite(img.convert('RGBA'),
                                sh.filter(ImageFilter.GaussianBlur(radius=10)))

    # === 袍身：手绘控制点，大写意人形轮廓 ===
    # 所有坐标相对于 (cx, neck_y) 计算，单位：像素*s
    # 从左肩外侧开始，顺时针：左袖外→左袖口→左下摆→下摆→右下摆→右袖口→右袖外→右肩→头侧
    sw = int(22*s)   # 半肩宽+袖厚
    rw = int(60*s)   # 下摆半宽
    bh = int(130*s)  # 袍身可见高度
    sl = int(35*s)   # 袖下垂长度（从肩到袖口）
    swag = int(12*s) # 风把右袖/摆向右推

    robe = [
        # 左肩外
        (cx - sw, neck_y + int(2*s)),
        # 左袖外沿（自然下垂，微向内收）
        (cx - sw - int(3*s), neck_y + int(sl*0.3)),
        (cx - sw - int(1*s), neck_y + int(sl*0.6)),
        # 左袖口
        (cx - sw + int(6*s), neck_y + sl + int(4*s)),
        # 左襟（袖内侧与袍身交汇处，腰线下开始展宽）
        (cx - sw + int(10*s), neck_y + sl + int(18*s)),
        (cx - int(rw*0.7), neck_y + int(bh*0.7)),
        (cx - rw + int(2*s), neck_y + bh - int(4*s)),
        # 左下摆角
        (cx - rw + int(8*s), feet_y),
        # 底缘（被风吹向右上方）
        (cx + int(rw*0.2) + swag, feet_y - int(3*s)),
        (cx + rw + swag - int(2*s), feet_y - int(1*s)),
        # 右下摆角
        (cx + rw + swag, feet_y),
        # 右襟
        (cx + int(rw*0.8) + swag, neck_y + int(bh*0.65)),
        (cx + sw + int(22*s) + swag, neck_y + sl + int(10*s)),
        # 右袖口（被风吹得略上扬）
        (cx + sw + int(28*s) + swag, neck_y + int(sl*0.5)),
        (cx + sw + int(18*s) + swag, neck_y + int(sl*0.2)),
        # 右袖外沿回到肩
        (cx + sw + int(6*s), neck_y + int(2*s)),
        # 右肩→颈→头右侧→头左侧→左肩
        (cx + int(head_r*0.9), neck_y - int(1*s)),
        (cx - int(head_r*0.9), neck_y - int(1*s)),
    ]

    d.polygon(robe, fill=(*PAPER, 250))

    # === 头 ===
    d.ellipse([cx-head_r, head_cy-head_r, cx+head_r, head_cy+head_r],
              fill=(*PAPER, 245))

    # === 发髻 ===
    br = int(5*s)
    d.ellipse([cx-br+int(1*s), head_cy-head_r-int(13*s),
               cx+br+int(1*s), head_cy-head_r],
              fill=(16,14,28,240))
    # 发簪（一点金）
    d.ellipse([cx+br-int(1*s), head_cy-head_r-int(7*s),
               cx+br+int(5*s), head_cy-head_r-int(2*s)],
              fill=(*GOLD, 190))

    # === 白色发带飘扬（主动势线）===
    rib = []
    for i in range(55):
        t = i/54
        rx = cx + int(1*s) + int(t*180*s) + int(20*s) + int(25*s*math.sin(t*3.2))
        ry = head_cy - int(1*s) - int(t*95*s) + int(20*s*math.cos(t*2.0))
        rib.append((rx, ry))
    for i in range(len(rib)-1):
        fade = 1 - i/len(rib)*0.7
        d.line([rib[i], rib[i+1]], fill=(*PAPER, int(225*fade)),
               width=max(1,int(2.5*s*(1-i/len(rib)*0.4))))

    # 大写意：省略腰带、佩剑等内部线条，仅以外轮廓传达人形

    # 对剑客轮廓施加极轻微模糊，消除硬几何边
    ov = ov.filter(ImageFilter.GaussianBlur(radius=0.8))

    return Image.alpha_composite(img.convert('RGBA'), ov)


# ---------------------------------------------------------------------------
# 山门
# ---------------------------------------------------------------------------
def draw_gate(img, cx, base_y, s=1.0):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    pw, ph, span = int(3*s), int(45*s), int(48*s)
    rh, rext = int(14*s), int(14*s)
    col = lerp_color(INK, MIST, 0.25)
    # 柱子
    d.rectangle([cx-span-pw, base_y-ph, cx-span, base_y], fill=(*col, 80))
    d.rectangle([cx+span, base_y-ph, cx+span+pw, base_y], fill=(*col, 80))
    # 额枋
    d.rectangle([cx-span-pw-4, base_y-ph-5, cx+span+pw+4, base_y-ph], fill=(*col, 85))
    d.rectangle([cx-span//2-2, base_y-ph//2-2, cx+span//2+2, base_y-ph//2+2], fill=(*col, 60))
    # 歇山顶
    rp = []
    for i in range(28):
        t = i/27
        rx = cx-span-pw-rext + int(t*2*(span+pw+rext))
        dx = (rx-cx)/(span+pw+rext)
        ry = base_y-ph - int(rh*(1-dx*dx)*(1+0.15*math.sin(t*3.14)))
        rp.append((rx, ry))
    d.polygon(rp + [(cx+span+pw+rext, base_y-ph), (cx-span-pw-rext, base_y-ph)],
              fill=(*col, 75))
    ov = ov.filter(ImageFilter.GaussianBlur(radius=3))
    return Image.alpha_composite(img.convert('RGBA'), ov)


# ---------------------------------------------------------------------------
# 金色地平线
# ---------------------------------------------------------------------------
def gold_horizon(img, y, intensity=1.0):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    for i in range(220):
        dist = abs((i-110)/220)
        a = int(55*intensity*math.exp(-dist*dist*4))
        d.line([(0,y-110+i),(W,y-110+i)], fill=(*GOLD, a))
    for i in range(50):
        off = i-10
        a = int(130*intensity*math.exp(-((i-10)/18)**2))
        d.line([(0,y+off),(W,y+off)], fill=(*GOLD, a))
    return Image.alpha_composite(img.convert('RGBA'),
                                 ov.filter(ImageFilter.GaussianBlur(radius=22)))


# ---------------------------------------------------------------------------
# 朱砂烟
# ---------------------------------------------------------------------------
def cinnabar_wisp(img, x, y, length=90):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    pts = []
    for i in range(35):
        t = i/34
        px = x + int(t*25) + int(15*math.sin(t*2.5+1.5))
        py = y - int(t*length) + int(10*math.cos(t*2+0.5))
        pts.append((px, py))
    glow = Image.new('RGBA', img.size, (0,0,0,0))
    gd = ImageDraw.Draw(glow)
    for px, py in pts[::2]:
        gd.ellipse([px-18,py-18,px+18,py+18], fill=(*CINNABAR, 14))
    glow = glow.filter(ImageFilter.GaussianBlur(radius=12))
    for i in range(len(pts)-1):
        fade = 1 - i/len(pts)*0.7
        d.line([pts[i], pts[i+1]], fill=(*CINNABAR, int(140*fade)),
               width=max(1,int(2.5*(1-i/len(pts)*0.4))))
    ov = ov.filter(ImageFilter.GaussianBlur(radius=1))
    img = Image.alpha_composite(img.convert('RGBA'), glow)
    return Image.alpha_composite(img, ov)


# ---------------------------------------------------------------------------
# 后期处理
# ---------------------------------------------------------------------------
def paper_texture(img, intensity=4):
    import numpy as np
    arr = np.array(img.convert('RGBA'), dtype=np.int16)
    noise = np.random.normal(0, intensity, arr.shape[:2]).astype(np.int16)
    for c in range(3):
        arr[:,:,c] = np.clip(arr[:,:,c] + noise, 0, 255)
    return Image.fromarray(arr.astype(np.uint8))


def vignette(img, strength=1.0):
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    for y in range(int(H*0.52)):
        t = y/(H*0.52)
        a = int(strength*130*math.exp(-t*2))
        d.line([(0,y),(W,y)], fill=(4,4,10,a))
    for y in range(int(H*0.65), H):
        t = (y-H*0.65)/(H*0.35)
        a = int(strength*100*t)
        d.line([(0,y),(W,y)], fill=(4,4,10,a))
    for x in range(int(W*0.1)):
        t = x/(W*0.1)
        a = int(strength*45*(1-t))
        d.line([(x,0),(x,H)], fill=(4,4,10,a))
        d.line([(W-1-x,0),(W-1-x,H)], fill=(4,4,10,a))
    return Image.alpha_composite(img.convert('RGBA'),
                                 ov.filter(ImageFilter.GaussianBlur(radius=55)))


def ink_drops(img, count=35):
    rng = random.Random(999)
    ov = Image.new('RGBA', img.size, (0,0,0,0))
    d = ImageDraw.Draw(ov)
    for _ in range(count):
        sx, sy = rng.randint(0,W), rng.randint(int(H*0.1), H)
        sr, sa = rng.randint(1,4), rng.randint(3,12)
        c = PAPER if rng.random()>0.6 else INK_DEEP
        d.ellipse([sx-sr,sy-sr,sx+sr,sy+sr], fill=(*c, sa))
    return Image.alpha_composite(img.convert('RGBA'),
                                 ov.filter(ImageFilter.GaussianBlur(radius=2)))


# ---------------------------------------------------------------------------
# 主合成
# ---------------------------------------------------------------------------
def compose():
    img = Image.new('RGBA', (W,H), (*SKY_TOP, 255))
    d = ImageDraw.Draw(img)
    # 天色三段渐变
    for y in range(H):
        t = y/H
        if t < 0.38:
            c = lerp_color(SKY_TOP, lerp_color(INK, MIST, 0.18), t/0.38)
        elif t < 0.55:
            c = lerp_color(lerp_color(INK, MIST, 0.18), lerp_color(INK, MIST, 0.3), (t-0.38)/0.17)
        else:
            c = lerp_color(lerp_color(INK, MIST, 0.3), INK_DEEP, (t-0.55)/0.45)
        d.line([(0,y),(W,y)], fill=(*c,255))

    # ======== 远山 ========
    hm1 = midpoint_displacement(W, int(H*0.48), 200, 0.58, 11, 9)
    # 添加几座尖峰
    add_sharp_peak(hm1, int(W*0.42), 120, 150, 0.5)
    add_sharp_peak(hm1, int(W*0.30), 70, 100, 0.4)
    add_sharp_peak(hm1, int(W*0.78), 60, 110, 0.4)
    img = draw_mountains(img, hm1, lerp_color(INK, MIST, 0.42), 55, 16)

    hm2 = midpoint_displacement(W, int(H*0.50), 220, 0.55, 22, 9)
    add_sharp_peak(hm2, int(W*0.52), 110, 130, 0.5)
    add_sharp_peak(hm2, int(W*0.25), 70, 90, 0.4)
    img = draw_mountains(img, hm2, lerp_color(INK, MIST, 0.32), 75, 12)

    hm3 = midpoint_displacement(W, int(H*0.53), 190, 0.53, 33, 9)
    add_sharp_peak(hm3, int(W*0.68), 90, 110, 0.45)
    img = draw_mountains(img, hm3, lerp_color(INK, MIST, 0.22), 95, 8)

    # 金色黎明
    img = gold_horizon(img, int(H*0.45), 1.4)

    # 远雾
    img = mist_band(img, int(H*0.51), 45, 0.22, 45)

    # ======== 中山 ========
    hm4 = midpoint_displacement(W, int(H*0.58), 220, 0.52, 44, 9)
    # 主门峰：山顶有山门，不宜过尖
    gate_peak_x = int(W*0.58)
    add_sharp_peak(hm4, gate_peak_x, 110, 140, 0.45)
    add_sharp_peak(hm4, int(W*0.20), 70, 90, 0.4)
    add_sharp_peak(hm4, int(W*0.85), 80, 100, 0.45)
    img = draw_mountains(img, hm4, lerp_color(INK, MIST, 0.12), 160, 5)

    # 山门在主峰顶端
    gate_base_y = hm4[gate_peak_x]
    img = draw_gate(img, gate_peak_x, gate_base_y, 0.55)

    # 朱砂烟
    img = cinnabar_wisp(img, gate_peak_x + 20, gate_base_y - 15, 80)

    img = mist_band(img, int(H*0.60), 30, 0.15, 30)

    hm5 = midpoint_displacement(W, int(H*0.64), 180, 0.50, 55, 9)
    add_sharp_peak(hm5, int(W*0.18), 80, 60, 0.6)
    img = draw_mountains(img, hm5, lerp_color(INK, PAPER, 0.05), 200, 2)

    img = mist_band(img, int(H*0.68), 25, 0.10, 25)

    # ======== 近山 ========
    hm6 = midpoint_displacement(W, int(H*0.74), 130, 0.48, 66, 9)
    img = draw_mountains(img, hm6, lerp_color(INK, PAPER, 0.01), 235, 1)

    # 前景地面
    hm_fg = midpoint_displacement(W, int(H*0.84), 18, 0.38, 77, 8, 0.05)
    img = draw_mountains(img, hm_fg, INK_DEEP, 252, 0)

    # 小径
    pov = Image.new('RGBA', img.size, (0,0,0,0))
    pd = ImageDraw.Draw(pov)
    pp = []
    for i in range(30):
        t = i/29
        pp.append((int(W*0.18 + t*W*0.15), int(H*0.88 - t*30 + 6*math.sin(t*2.8))))
    for i in range(len(pp)-1):
        pd.line([pp[i], pp[i+1]], fill=(*PAPER, 25), width=2)
    img = Image.alpha_composite(img, pov.filter(ImageFilter.GaussianBlur(radius=4)))

    # ======== 剑客 ========
    sx = int(W*0.26)
    sy = hm_fg[sx] + 1
    img = draw_swordsman(img, sx, sy, 1.0)

    # 近地雾
    img = mist_band(img, int(H*0.79), 55, 0.13, 45)
    img = mist_band(img, int(H*0.91), 40, 0.08, 35)

    # 飘浮雾粒
    rng = random.Random(55)
    part = Image.new('RGBA', img.size, (0,0,0,0))
    pd = ImageDraw.Draw(part)
    for _ in range(120):
        px, py = rng.randint(0,W), rng.randint(int(H*0.15), H)
        pr, pa = rng.randint(1,3), rng.randint(5,22)
        pd.ellipse([px-pr,py-pr,px+pr,py+pr], fill=(*PAPER, pa))
    img = Image.alpha_composite(img, part.filter(ImageFilter.GaussianBlur(radius=2)))

    # 后期
    img = ink_drops(img, 25)
    img = paper_texture(img, 3)
    img = vignette(img, 1.0)
    img = img.filter(ImageFilter.GaussianBlur(radius=0.4))

    final = img.convert('RGB')
    final.save(OUTPUT, 'PNG')
    print(f"✅ 已保存: {OUTPUT}")
    print(f"尺寸: {final.size}")


if __name__ == '__main__':
    compose()
