#!/usr/bin/env python3
"""Generate isometric diamond tileset for Jiangnan water-town ground (v5 - fixed geometry)."""

from __future__ import annotations

import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

SEED = 42
random.seed(SEED)

TILE_W = 64
TILE_H = 32
COLS = 4
ROWS = 4
SHEET_W = COLS * TILE_W
SHEET_H = ROWS * TILE_H
SCALE = 4
PW = TILE_W * SCALE
PH = TILE_H * SCALE

OUT_DIR = Path('/Users/bytedance/my-game/assets/generated/maps/jiangnan-riverside')
FINAL_PATH = OUT_DIR / 'tilesets' / 'jiangnan_ground_tiles.png'

C = {
    'stone1': (118, 126, 132),
    'stone2': (132, 140, 146),
    'stone3': (148, 155, 160),
    'stone_dk': (95, 102, 108),
    'stone_wet': (82, 90, 98),
    'mud1': (108, 90, 68),
    'mud2': (125, 105, 80),
    'mud3': (90, 72, 55),
    'mud_wet': (72, 58, 42),
    'moss1': (95, 122, 82),
    'moss2': (115, 142, 95),
    'moss3': (75, 100, 65),
    'moss4': (135, 160, 110),
    'wsh1': (115, 170, 152),
    'wsh2': (135, 190, 168),
    'wsh3': (95, 148, 132),
    'wsh_hl': (165, 210, 188),
    'wdp1': (62, 108, 112),
    'wdp2': (78, 128, 130),
    'wdp3': (50, 92, 98),
    'wet_edge': (68, 62, 50),
    'shore_foam': (180, 215, 200),
}


def diamond_points(w, h):
    cx, cy = w / 2, h / 2
    hw, hh = w / 2, h / 2
    return [(cx, cy - hh + 0.5), (cx + hw - 0.5, cy), (cx, cy + hh - 0.5), (cx - hw + 0.5, cy)]


def diamond_mask(w, h, shrink=0):
    m = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(m)
    cx, cy = w / 2, h / 2
    hw, hh = w / 2 - shrink, h / 2 - shrink
    pts = [(cx, cy - hh + 0.5), (cx + hw - 0.5, cy), (cx, cy + hh - 0.5), (cx - hw + 0.5, cy)]
    d.polygon(pts, fill=255)
    return m


def apply_diamond_mask(img):
    w, h = img.size
    dm = diamond_mask(w, h, shrink=0)
    result = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    result.paste(img, (0, 0), dm)
    return result


def fill_stone(base_img):
    w, h = base_img.size
    d = ImageDraw.Draw(base_img)
    cx, cy = w // 2, h // 2
    pts = diamond_points(w, h)
    d.polygon(pts, fill=C['stone2'])
    px = base_img.load()
    hw, hh = w / 2, h / 2
    for y in range(h):
        for x in range(w):
            dx = abs(x - cx) / hw
            dy = abs(y - cy) / hh
            if dx + dy > 0.96:
                continue
            if random.random() < 0.4:
                v = random.randint(-20, 20)
                r, g, b = C['stone2']
                px[x, y] = (max(0, min(255, r+v)), max(0, min(255, g+v)), max(0, min(255, b+v)), 255)
    for _ in range(6):
        sx, sy = random.randint(w//6, 5*w//6), random.randint(h//4, 3*h//4)
        length = random.randint(w//10, w//3)
        ang = random.uniform(-0.8, 0.8) + math.pi/4
        for i in range(length):
            pxi, pyi = int(sx + i*math.cos(ang)), int(sy + i*math.sin(ang)*0.5)
            if 0 <= pxi < w and 0 <= pyi < h:
                orig = base_img.getpixel((pxi, pyi))
                if orig[3] > 0:
                    d.point((pxi, pyi), fill=C['stone_dk'] + (random.randint(50, 120),))
            ang += random.uniform(-0.25, 0.25)
    for _ in range(10):
        hx, hy = random.randint(w//10, 9*w//10), random.randint(h//5, 4*h//5)
        rw, rh = random.randint(3, w//7), max(1, random.randint(2, w//14))
        d.ellipse((hx-rw, hy-rh, hx+rw, hy+rh), fill=C['stone3']+(random.randint(25,70),))
    return apply_diamond_mask(base_img)


def fill_mud(base_img):
    w, h = base_img.size
    d = ImageDraw.Draw(base_img)
    cx, cy = w // 2, h // 2
    pts = diamond_points(w, h)
    d.polygon(pts, fill=C['mud2'])
    px = base_img.load()
    hw, hh = w / 2, h / 2
    for y in range(h):
        for x in range(w):
            dx = abs(x - cx) / hw
            dy = abs(y - cy) / hh
            if dx + dy > 0.96:
                continue
            if random.random() < 0.5:
                v = random.randint(-22, 22)
                r, g, b = C['mud2']
                px[x,y] = (max(0,min(255,r+v)), max(0,min(255,g+v)), max(0,min(255,b+v)), 255)
    for _ in range(8):
        px, py = random.randint(w//6,5*w//6), random.randint(h//4,3*h//4)
        r = random.randint(2, max(3, w//10))
        d.ellipse((px-r, py-r//2, px+r, py+r//2), fill=C['mud_wet']+(random.randint(50,100),))
    return apply_diamond_mask(base_img)


def fill_moss(base_img):
    fill_mud(base_img)
    w, h = base_img.size
    d = ImageDraw.Draw(base_img)
    for _ in range(25):
        mcx, mcy = random.randint(w//10,9*w//10), random.randint(h//5,4*h//5)
        mr = random.randint(2, max(3, w//7))
        mc = random.choice([C['moss1'], C['moss2'], C['moss3'], C['moss4']])
        for dy in range(-mr, mr):
            for dx in range(-mr*2, mr*2):
                if dx*dx/((mr*2)**2) + dy*dy/(mr*mr) <= 1:
                    mx, my = mcx+dx, mcy+dy
                    if 0 <= mx < w and 0 <= my < h:
                        orig = base_img.getpixel((mx, my))
                        if orig[3] > 0 and random.random() < 0.65:
                            base_img.putpixel((mx, my), mc+(random.randint(100,200),))
    for _ in range(18):
        gx, gy = random.randint(w//6,5*w//6), random.randint(h//4,3*h//4)
        gc = random.choice([C['moss2'], C['moss4']])
        for _ in range(random.randint(2,5)):
            bx = gx + random.randint(-4,4)
            for s in range(random.randint(2,6)):
                bpx, bpy = bx+random.randint(-1,1), gy-s
                if 0<=bpx<w and 0<=bpy<h:
                    orig = base_img.getpixel((bpx,bpy))
                    if orig[3]>0:
                        base_img.putpixel((bpx,bpy), gc+(220,))
    return apply_diamond_mask(base_img)


def fill_water(base_img, deep=False):
    w, h = base_img.size
    d = ImageDraw.Draw(base_img)
    cx, cy = w//2, h//2
    pts = diamond_points(w, h)
    base_c = C['wdp2'] if deep else C['wsh2']
    lt_c = C['wdp1'] if deep else C['wsh3']
    hl_c = C['wsh_hl']
    d.polygon(pts, fill=base_c)
    px = base_img.load()
    hw, hh = w/2, h/2
    for y in range(h):
        for x in range(w):
            dx = abs(x - cx) / hw
            dy = abs(y - cy) / hh
            if dx + dy > 0.96:
                continue
            if random.random() < 0.35:
                v = random.randint(-14, 14)
                r, g, b = base_c
                px[x,y] = (max(0,min(255,r+v)), max(0,min(255,g+v)), max(0,min(255,b+v)), 255)
    for _ in range(6 if not deep else 3):
        rx, ry = random.randint(w//5,4*w//5), random.randint(h//4,3*h//4)
        rw, rh = random.randint(w//12,w//4), max(1, random.randint(1, w//12))
        d.ellipse((rx-rw,ry-rh,rx+rw,ry+rh), outline=lt_c+(random.randint(30,70),), width=max(1, SCALE//4))
    for _ in range(4 if not deep else 2):
        hx, hy = random.randint(w//4,3*w//4), random.randint(h//3,2*h//3)
        hlw, hlh = random.randint(2, max(3, w//12)), max(1, random.randint(1, w//20))
        d.ellipse((hx-hlw,hy-hlh,hx+hlw,hy+hlh), fill=hl_c+(random.randint(50,100),))
    return apply_diamond_mask(base_img)


def _direction_field(w, h, direction, rng):
    """Scalar field: 0=fully water (at edge), 1=fully land (opposite edge).
    Directions: 'NE' (north/up edge), 'SE' (east/right), 'SW' (south/bottom), 'NW' (west/left)."""
    cx, cy = w/2, h/2
    hw, hh = w/2, h/2
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = (X - cx + 0.5) / hw
    dy = (Y - cy + 0.5) / hh
    dd = np.abs(dx) + np.abs(dy)
    if direction == 'NE':
        f = (1.0 - (dx - dy)) / 2.0
    elif direction == 'SE':
        f = (1.0 - (dx + dy)) / 2.0
    elif direction == 'SW':
        f = (1.0 - (-dx + dy)) / 2.0
    elif direction == 'NW':
        f = (1.0 - (-dx - dy)) / 2.0
    else:
        raise ValueError(direction)
    x_wave = np.sin(X * (2*math.pi/w) * 2.5 + rng.uniform(0, math.pi*2)) * 0.07
    y_wave = np.sin(Y * (2*math.pi/h) * 2 + rng.uniform(0, math.pi*2)) * 0.06
    noise_rand = rng.rand(h, w).astype(np.float32) * 0.10 - 0.05
    noise = x_wave + y_wave + noise_rand
    f = f + noise
    f = np.clip(f, 0, 1)
    f[dd > 1.0] = 1.0
    return f


def make_water_mask(w, h, water_dirs, rng):
    if not water_dirs:
        return np.zeros((h, w), dtype=np.uint8), np.zeros((h, w), dtype=np.uint8)
    if len(water_dirs) == 4:
        cx, cy = w/2, h/2
        hw, hh = w/2, h/2
        Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
        dx = (X - cx + 0.5) / hw
        dy = (Y - cy + 0.5) / hh
        dd = np.abs(dx) + np.abs(dy)
        r = 0.25
        land_field = np.clip((dd - r) / (1.0 - r), 0, 1)
        wave = np.sin(X*0.1 + Y*0.15 + rng.uniform(0,10)) * 0.08 + rng.rand(h,w).astype(np.float32)*0.06 - 0.03
        land_field = np.clip(land_field + wave, 0, 1)
        field = 1.0 - land_field
        field[dd > 1.0] = 1.0
    else:
        fields = []
        for d in water_dirs:
            fields.append(_direction_field(w, h, d, rng))
        field = np.minimum.reduce(fields)
        cx, cy = w/2, h/2
        hw, hh = w/2, h/2
        Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
        dx = (X - cx + 0.5) / hw
        dy = (Y - cy + 0.5) / hh
        dd = np.abs(dx) + np.abs(dy)
        field[dd > 1.0] = 1.0
    threshold = 0.50
    water_binary = np.where(field < threshold, 255, 0).astype(np.uint8)
    field_img = Image.fromarray(np.clip(field * 255, 0, 255).astype(np.uint8))
    field_blur = field_img.filter(ImageFilter.GaussianBlur(radius=max(1, w/80)))
    field_arr = np.array(field_blur).astype(np.float32) / 255.0
    wet_band_w = 0.18
    edge_binary = np.where((field_arr > threshold - wet_band_w) & (field_arr < threshold + 0.06), 200, 0).astype(np.uint8)
    dm = np.array(diamond_mask(w, h))
    edge_binary[dm == 0] = 0
    water_binary[dm == 0] = 0
    return water_binary, edge_binary


def make_shore(water_dirs, land_type='stone', deep=False, seed_offset=0):
    rng = np.random.RandomState(SEED + seed_offset)
    random.seed(SEED + seed_offset)
    land_img = Image.new('RGBA', (PW, PH), (0,0,0,0))
    water_img = Image.new('RGBA', (PW, PH), (0,0,0,0))
    if land_type == 'mud':
        fill_mud(land_img)
    elif land_type == 'moss':
        fill_moss(land_img)
    else:
        fill_stone(land_img)
    fill_water(water_img, deep=deep)
    water_mask, edge_mask = make_water_mask(PW, PH, water_dirs, rng)
    land_mask_arr = 255 - water_mask
    dm_arr = np.array(diamond_mask(PW, PH))
    land_mask_arr[dm_arr == 0] = 0
    water_mask[dm_arr == 0] = 0
    edge_mask[dm_arr == 0] = 0
    tile = Image.new('RGBA', (PW, PH), (0,0,0,0))
    tile.paste(water_img, (0,0), Image.fromarray(water_mask))
    tile.paste(land_img, (0,0), Image.fromarray(land_mask_arr))
    wet_color = C['stone_wet'] if land_type == 'stone' else (C['mud_wet'] if land_type == 'mud' else C['moss3'])
    wet_band = Image.new('RGBA', (PW, PH), wet_color+(90,))
    tile.paste(wet_band, (0,0), Image.fromarray(edge_mask))
    return apply_diamond_mask(tile)


def make_wet_transition():
    tile = Image.new('RGBA', (PW, PH), (0,0,0,0))
    fill_mud(tile)
    d = ImageDraw.Draw(tile)
    w, h = PW, PH
    for _ in range(20):
        cx, cy = random.randint(w//10,9*w//10), random.randint(h//5,4*h//5)
        rw, rh = random.randint(w//20,w//5), max(1, random.randint(1, w//10))
        wc = random.choice([C['wsh1'], C['wsh2'], C['mud_wet']])
        d.ellipse((cx-rw,cy-rh,cx+rw,cy+rh), fill=wc+(random.randint(60,150),))
    return apply_diamond_mask(tile)


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR/'raw').mkdir(exist_ok=True)
    (OUT_DIR/'tilesets').mkdir(exist_ok=True)
    (OUT_DIR/'previews').mkdir(exist_ok=True)
    sheet = Image.new('RGBA', (SHEET_W, SHEET_H), (0,0,0,0))
    tiles_list = []
    def full_stone(so):
        random.seed(SEED+so); return fill_stone(Image.new('RGBA',(PW,PH),(0,0,0,0)))
    def full_mud(so):
        random.seed(SEED+so); return fill_mud(Image.new('RGBA',(PW,PH),(0,0,0,0)))
    def full_moss(so):
        random.seed(SEED+so); return fill_moss(Image.new('RGBA',(PW,PH),(0,0,0,0)))
    def full_water(deep, so):
        random.seed(SEED+so); return fill_water(Image.new('RGBA',(PW,PH),(0,0,0,0)), deep=deep)
    tile_defs = [
        ('bluestone',       lambda: full_stone(100)),
        ('wet_mud',         lambda: full_mud(200)),
        ('moss_ground',     lambda: full_moss(300)),
        ('bluestone_worn',  lambda: full_stone(400)),
        ('shallow_water',   lambda: full_water(False, 500)),
        ('deep_water',      lambda: full_water(True, 600)),
        ('shore_N',         lambda: make_shore({'NE'}, 'stone', seed_offset=700)),
        ('shore_E',         lambda: make_shore({'SE'}, 'stone', seed_offset=800)),
        ('shore_S',         lambda: make_shore({'SW'}, 'stone', seed_offset=900)),
        ('shore_W',         lambda: make_shore({'NW'}, 'stone', seed_offset=1000)),
        ('corner_inner_NE', lambda: make_shore({'NE','SE'}, 'stone', seed_offset=1100)),
        ('corner_inner_SE', lambda: make_shore({'SE','SW'}, 'stone', seed_offset=1200)),
        ('corner_inner_SW', lambda: make_shore({'SW','NW'}, 'stone', seed_offset=1300)),
        ('corner_inner_NW', lambda: make_shore({'NW','NE'}, 'stone', seed_offset=1400)),
        ('wet_mud_puddles', lambda: (random.seed(SEED+1500), make_wet_transition())[1]),
        ('mossy_islet',     lambda: make_shore({'NE','SE','SW','NW'}, 'moss', seed_offset=1600)),
    ]
    print("Generating tiles...")
    for idx, (name, gen) in enumerate(tile_defs):
        tile_big = gen()
        tile_small = tile_big.resize((TILE_W, TILE_H), Image.NEAREST)
        tiles_list.append((name, tile_small))
        row, col = idx // COLS, idx % COLS
        x, y = col * TILE_W, row * TILE_H
        sheet.paste(tile_small, (x, y), tile_small)
        print(f"  Tile {idx:2d} [{name}]")
    sheet.save(FINAL_PATH)
    print(f"Final sheet: {FINAL_PATH} ({SHEET_W}x{SHEET_H})")
    contact = Image.new('RGB', (SHEET_W*3, SHEET_H*3), (35,38,45))
    for r in range(ROWS):
        for c in range(COLS):
            tidx = r*COLS+c
            _, tile = tiles_list[tidx]
            tbig = tile.resize((TILE_W*3, TILE_H*3), Image.NEAREST)
            contact.paste(tbig, (c*TILE_W*3, r*TILE_H*3), tbig)
    contact.save(OUT_DIR/'previews'/'tileset_contact_sheet.png')
    print("Contact sheet saved.")
    import json
    meta = {
        "tile_width": TILE_W,
        "tile_height": TILE_H,
        "columns": COLS,
        "rows": ROWS,
        "projection": "isometric_diamond_2_to_1",
        "tiles": [
            {"id":0,"name":"bluestone","category":"ground","walkable":True},
            {"id":1,"name":"wet_mud","category":"ground","walkable":True},
            {"id":2,"name":"moss_ground","category":"ground","walkable":True},
            {"id":3,"name":"bluestone_worn","category":"ground","walkable":True},
            {"id":4,"name":"shallow_water","category":"water","walkable":False},
            {"id":5,"name":"deep_water","category":"water","walkable":False},
            {"id":6,"name":"shore_N","category":"shore","walkable":True,"water_neighbors":["NE"]},
            {"id":7,"name":"shore_E","category":"shore","walkable":True,"water_neighbors":["SE"]},
            {"id":8,"name":"shore_S","category":"shore","walkable":True,"water_neighbors":["SW"]},
            {"id":9,"name":"shore_W","category":"shore","walkable":True,"water_neighbors":["NW"]},
            {"id":10,"name":"corner_inner_NE","category":"shore_inner_corner","walkable":True,"water_neighbors":["NE","SE"]},
            {"id":11,"name":"corner_inner_SE","category":"shore_inner_corner","walkable":True,"water_neighbors":["SE","SW"]},
            {"id":12,"name":"corner_inner_SW","category":"shore_inner_corner","walkable":True,"water_neighbors":["SW","NW"]},
            {"id":13,"name":"corner_inner_NW","category":"shore_inner_corner","walkable":True,"water_neighbors":["NW","NE"]},
            {"id":14,"name":"wet_mud_puddles","category":"transition","walkable":True},
            {"id":15,"name":"mossy_islet","category":"islet","walkable":True},
        ]
    }
    with open(OUT_DIR/'tilesets'/'jiangnan_ground_tiles.json', 'w', encoding='utf-8') as f:
        json.dump(meta, f, indent=2, ensure_ascii=False)
    print("Metadata JSON saved.")
    print("Done!")


if __name__ == '__main__':
    main()
