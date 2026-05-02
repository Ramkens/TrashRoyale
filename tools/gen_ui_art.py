#!/usr/bin/env python3
"""Generates main-menu/UI artwork (background, buttons, banner, icons) using PIL.

All assets are original, designed in a Clash-Royale-inspired style but NOT
copied. Output: Assets/Resources/UI/*.png
"""
import os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageOps, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "UI")
os.makedirs(OUT, exist_ok=True)

# ---------- helpers ----------

def vert_grad(w, h, top, bottom):
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        t = y / max(1, h - 1)
        r = int(top[0] * (1 - t) + bottom[0] * t)
        g = int(top[1] * (1 - t) + bottom[1] * t)
        b = int(top[2] * (1 - t) + bottom[2] * t)
        for x in range(w):
            px[x, y] = (r, g, b)
    return img


def round_rect_mask(w, h, radius):
    img = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=255)
    return img


def add_inner_shadow(img, alpha=120, radius=18):
    w, h = img.size
    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    d.rounded_rectangle((4, 4, w - 5, h - 5), radius=radius - 2, outline=(0, 0, 0, alpha), width=6)
    overlay = overlay.filter(ImageFilter.GaussianBlur(3))
    img.alpha_composite(overlay)


# ---------- 1. Menu background ----------

def make_menu_bg(w=1080, h=1920):
    # Sky: deep blue to purple
    sky = vert_grad(w, h, (35, 50, 100), (80, 40, 110))

    # Add stars / specks
    d = ImageDraw.Draw(sky)
    rng = random.Random(13)
    for _ in range(220):
        x = rng.randint(0, w - 1)
        y = rng.randint(0, int(h * 0.55))
        r = rng.choice([1, 1, 1, 2, 2, 3])
        a = rng.randint(120, 255)
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 230))

    # Sun glow circle
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((w // 2 - 380, h // 4 - 380, w // 2 + 380, h // 4 + 380), fill=(255, 230, 150, 90))
    glow = glow.filter(ImageFilter.GaussianBlur(80))

    out = Image.new("RGBA", (w, h))
    out.paste(sky, (0, 0))
    out.alpha_composite(glow)

    # Distant castle silhouette (dark blue)
    castle = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cd = ImageDraw.Draw(castle)
    base_y = int(h * 0.62)
    castle_color = (25, 30, 70, 230)
    # Main wall
    cd.rectangle((100, base_y, w - 100, base_y + 220), fill=castle_color)
    # Battlements
    for x in range(100, w - 100, 50):
        cd.rectangle((x, base_y - 25, x + 30, base_y + 5), fill=castle_color)
    # Towers
    for cx in [180, w // 2 - 220, w // 2 + 220, w - 180]:
        cd.rectangle((cx - 60, base_y - 220, cx + 60, base_y + 80), fill=castle_color)
        # tower top battlements
        for tx in range(cx - 60, cx + 60, 25):
            cd.rectangle((tx, base_y - 250, tx + 15, base_y - 220), fill=castle_color)
        # roof
        cd.polygon([(cx - 70, base_y - 220), (cx, base_y - 360), (cx + 70, base_y - 220)], fill=(140, 50, 70, 230))
    # Central keep
    cx0 = w // 2
    cd.rectangle((cx0 - 130, base_y - 320, cx0 + 130, base_y + 80), fill=castle_color)
    cd.polygon([(cx0 - 145, base_y - 320), (cx0, base_y - 500), (cx0 + 145, base_y - 320)], fill=(180, 60, 80, 230))

    out.alpha_composite(castle)

    # Ground / arena platform
    ground = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd2 = ImageDraw.Draw(ground)
    gy = base_y + 80
    # green grass
    gd2.rectangle((0, gy, w, h), fill=(45, 110, 55, 255))
    # darker stripes for grass effect
    for sy in range(gy, h, 60):
        gd2.rectangle((0, sy, w, sy + 30), fill=(55, 130, 65, 255))
    # path stones in front
    for sx in range(40, w - 40, 90):
        gd2.rounded_rectangle((sx, h - 200, sx + 70, h - 140), radius=10, fill=(170, 165, 150, 255))

    out.alpha_composite(ground)

    # Vignette
    vignette = Image.new("L", (w, h), 0)
    vd = ImageDraw.Draw(vignette)
    vd.ellipse((-w // 3, -h // 3, w + w // 3, h + h // 3), fill=255)
    vignette = vignette.filter(ImageFilter.GaussianBlur(180))
    vmask = ImageOps.invert(vignette)
    dark = Image.new("RGBA", (w, h), (0, 0, 0, 200))
    out.paste(dark, (0, 0), vmask)

    out.convert("RGB").save(os.path.join(OUT, "menu_bg.png"), "PNG")
    print("menu_bg.png saved")


# ---------- 2. Gold button (9-slice friendly) ----------

def make_button_gold(w=600, h=180, radius=40, fname="btn_gold.png"):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, radius)
    grad = vert_grad(w, h, (255, 215, 80), (200, 130, 30))
    img.paste(grad, (0, 0), mask)

    d = ImageDraw.Draw(img)
    # Top highlight
    hl_mask = round_rect_mask(w - 20, h // 2 - 14, radius - 12)
    hl = Image.new("RGBA", hl_mask.size, (255, 250, 200, 110))
    img.paste(hl, (10, 10), hl_mask)

    # Outer dark border
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, outline=(80, 40, 10, 255), width=8)
    # Inner gold border
    d.rounded_rectangle((10, 10, w - 11, h - 11), radius=radius - 8, outline=(255, 240, 160, 220), width=4)

    img.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


def make_button_blue(w=600, h=180, radius=40, fname="btn_blue.png"):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, radius)
    grad = vert_grad(w, h, (90, 180, 255), (40, 90, 200))
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    hl_mask = round_rect_mask(w - 20, h // 2 - 14, radius - 12)
    hl = Image.new("RGBA", hl_mask.size, (220, 240, 255, 110))
    img.paste(hl, (10, 10), hl_mask)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, outline=(15, 25, 60, 255), width=8)
    d.rounded_rectangle((10, 10, w - 11, h - 11), radius=radius - 8, outline=(180, 220, 255, 220), width=4)
    img.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


# ---------- 3. Banner (player profile) ----------

def make_banner(w=1000, h=210, fname="banner_blue.png"):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, 30)
    grad = vert_grad(w, h, (45, 70, 140), (20, 30, 90))
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    # Decorative gold border
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=30, outline=(220, 175, 60, 255), width=6)
    d.rounded_rectangle((9, 9, w - 10, h - 10), radius=24, outline=(255, 230, 130, 200), width=2)
    # Header strip on top
    strip_mask = round_rect_mask(w - 40, 30, 14)
    strip = Image.new("RGBA", strip_mask.size, (0, 0, 0, 100))
    img.paste(strip, (20, 16), strip_mask)
    img.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


# ---------- 4. Logo plate ----------

def make_logo_plate(w=900, h=240, fname="logo_plate.png"):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, 40)
    grad = vert_grad(w, h, (140, 40, 60), (60, 15, 30))
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=40, outline=(255, 215, 80, 255), width=8)
    d.rounded_rectangle((10, 10, w - 11, h - 11), radius=32, outline=(255, 240, 170, 220), width=3)
    img.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


# ---------- 5. Arena tile (grass) ----------

def make_arena_tile(w=512, h=512, fname="arena_grass.png"):
    base = Image.new("RGB", (w, h), (60, 130, 65))
    d = ImageDraw.Draw(base)
    rng = random.Random(7)
    # Soft mowed lawn stripes (alternating light/dark green bands).
    band_h = 48
    for i, y in enumerate(range(0, h, band_h)):
        c = (74, 142, 75) if i % 2 == 0 else (60, 128, 64)
        d.rectangle((0, y, w, y + band_h), fill=c)
    # A couple of subtle darker tufts (no rainbow flower confetti).
    for _ in range(18):
        x = rng.randint(0, w - 1)
        y = rng.randint(0, h - 1)
        c = (52, 116, 56)
        d.ellipse((x, y, x + 3, y + 3), fill=c)
    base.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


def make_arena_river(w=1024, h=128, fname="arena_river.png"):
    base = vert_grad(w, h, (60, 130, 220), (30, 80, 180))
    d = ImageDraw.Draw(base)
    rng = random.Random(11)
    for _ in range(120):
        x = rng.randint(0, w - 30)
        y = rng.randint(0, h - 6)
        d.rectangle((x, y, x + 20, y + 3), fill=(180, 220, 255))
    base.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


def make_bridge(w=256, h=128, fname="arena_bridge.png"):
    base = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(base)
    # plank background
    d.rounded_rectangle((4, 4, w - 5, h - 5), radius=12, fill=(150, 100, 60, 255))
    # planks
    for x in range(10, w - 10, 30):
        d.rectangle((x, 8, x + 22, h - 8), fill=(180, 130, 80, 255))
        d.rectangle((x, 8, x + 22, h - 8), outline=(80, 50, 20, 255), width=2)
    # border
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=12, outline=(60, 40, 20, 255), width=4)
    base.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


# ---------- 6. Brick tower (top-down) ----------

def make_tower_brick(w=256, h=256, fname="tower_brick.png"):
    base = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, 28)
    bricks = Image.new("RGB", (w, h), (180, 90, 60))
    d = ImageDraw.Draw(bricks)
    bw, bh = 36, 18
    for row, y in enumerate(range(0, h, bh)):
        offset = (row % 2) * (bw // 2)
        for x in range(-bw, w + bw, bw):
            d.rectangle((x + offset, y, x + offset + bw - 2, y + bh - 2), fill=(200, 110, 70))
            d.rectangle((x + offset, y, x + offset + bw - 2, y + bh - 2), outline=(110, 50, 30), width=1)
    base.paste(bricks, (0, 0), mask)
    d2 = ImageDraw.Draw(base)
    d2.rounded_rectangle((0, 0, w - 1, h - 1), radius=28, outline=(80, 30, 15, 255), width=6)
    base.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


def make_tower_king(w=320, h=320, fname="tower_king.png"):
    base = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    mask = round_rect_mask(w, h, 36)
    grad = vert_grad(w, h, (200, 175, 90), (130, 100, 40))
    base.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(base)
    # Crown emblem on top
    cx, cy = w // 2, h // 2
    d.polygon([(cx - 80, cy - 30), (cx - 50, cy + 30), (cx + 50, cy + 30), (cx + 80, cy - 30),
               (cx + 50, cy - 10), (cx, cy - 50), (cx - 50, cy - 10)], fill=(255, 220, 90))
    d.polygon([(cx - 80, cy - 30), (cx - 50, cy + 30), (cx + 50, cy + 30), (cx + 80, cy - 30),
               (cx + 50, cy - 10), (cx, cy - 50), (cx - 50, cy - 10)], outline=(120, 70, 20), width=4)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=36, outline=(60, 40, 10, 255), width=8)
    base.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


# ---------- 7. Elixir bubble ----------

def make_elixir_bubble(d_pix=128, fname="elixir_bubble.png"):
    img = Image.new("RGBA", (d_pix, d_pix), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((0, 0, d_pix - 1, d_pix - 1), fill=(180, 60, 200, 255))
    d.ellipse((6, 6, d_pix - 7, d_pix - 7), outline=(255, 220, 255, 220), width=4)
    # Highlight
    d.ellipse((d_pix * 0.18, d_pix * 0.12, d_pix * 0.5, d_pix * 0.4), fill=(255, 220, 255, 200))
    img.save(os.path.join(OUT, fname), "PNG")
    print(f"{fname} saved")


if __name__ == "__main__":
    make_menu_bg()
    make_button_gold()
    make_button_blue()
    make_banner()
    make_logo_plate()
    make_arena_tile()
    make_arena_river()
    make_bridge()
    make_tower_brick()
    make_tower_king()
    make_elixir_bubble()
    print("All UI art generated.")
