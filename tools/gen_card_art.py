"""Procedural card-art generator for new units.

Style mirrors the existing CardArt PNGs: 512x768 RGBA portrait card with
a deep purple border, rounded gold inset frame, elixir cost circle in
the top-left, name banner at the bottom, and a stylized illustration of
the unit in the central area.

Reads card metadata from Assets/Resources/Cards/cards.json so cost and
display name stay in sync.
"""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import json
import os
import math

W, H = 512, 768
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARDS_JSON = os.path.join(ROOT, "Assets/Resources/Cards/cards.json")
OUT_DIR = os.path.join(ROOT, "Assets/Resources/CardArt")

PURPLE_DARK = (51, 33, 110)
PURPLE_BORDER = (37, 23, 86)
GOLD = (218, 174, 65)
GOLD_LIGHT = (255, 220, 130)
SCENE_TOP = (50, 38, 100)
SCENE_BOTTOM = (24, 17, 56)
NAME_BG = (51, 33, 110)
ELIXIR_PINK = (227, 65, 184)


def find_font(size):
    candidates = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    for p in candidates:
        if os.path.isfile(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def rounded_rect(draw, box, radius, fill=None, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def make_scene_gradient():
    img = Image.new("RGB", (W, H), SCENE_BOTTOM)
    px = img.load()
    for y in range(H):
        t = y / (H - 1)
        r = int(SCENE_TOP[0] + (SCENE_BOTTOM[0] - SCENE_TOP[0]) * t)
        g = int(SCENE_TOP[1] + (SCENE_BOTTOM[1] - SCENE_TOP[1]) * t)
        b = int(SCENE_TOP[2] + (SCENE_BOTTOM[2] - SCENE_TOP[2]) * t)
        for x in range(W):
            px[x, y] = (r, g, b)
    return img


def draw_frame(card):
    base = Image.new("RGBA", (W, H), PURPLE_BORDER + (255,))
    bd = ImageDraw.Draw(base)
    rounded_rect(bd, (8, 8, W - 8, H - 8), 36, fill=PURPLE_BORDER + (255,), outline=GOLD + (255,), width=6)
    rounded_rect(bd, (24, 100, W - 24, H - 160), 28, fill=SCENE_TOP + (255,))
    scene = make_scene_gradient().convert("RGBA")
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle((24, 100, W - 24, H - 160), 28, fill=255)
    base.paste(scene, (0, 0), mask)
    bd2 = ImageDraw.Draw(base)
    rounded_rect(bd2, (24, 100, W - 24, H - 160), 28, outline=GOLD + (255,), width=4)
    rounded_rect(bd2, (40, H - 150, W - 40, H - 28), 22, fill=NAME_BG + (255,), outline=GOLD + (255,), width=4)

    name_font = find_font(48)
    desc_font = find_font(20)
    name = card.get("displayName", card["id"])
    bbox = bd2.textbbox((0, 0), name, font=name_font)
    nw = bbox[2] - bbox[0]
    nh = bbox[3] - bbox[1]
    bd2.text(((W - nw) / 2, H - 142), name, font=name_font, fill=GOLD_LIGHT + (255,))
    desc = card.get("description", "")
    if desc:
        words = desc.split()
        lines = []
        cur = ""
        max_w = W - 100
        for w in words:
            trial = (cur + " " + w).strip()
            tb = bd2.textbbox((0, 0), trial, font=desc_font)
            if tb[2] - tb[0] > max_w and cur:
                lines.append(cur)
                cur = w
            else:
                cur = trial
        if cur:
            lines.append(cur)
        lines = lines[:2]
        y = H - 80
        for ln in lines:
            tb = bd2.textbbox((0, 0), ln, font=desc_font)
            tw = tb[2] - tb[0]
            bd2.text(((W - tw) / 2, y), ln, font=desc_font, fill=(220, 220, 240, 255))
            y += 24

    cost = card.get("elixirCost", 0)
    cx, cy, cr = 60, 70, 50
    bd2.ellipse((cx - cr, cy - cr, cx + cr, cy + cr), fill=ELIXIR_PINK + (255,), outline=(255, 255, 255, 255), width=4)
    cost_font = find_font(56)
    bb = bd2.textbbox((0, 0), str(cost), font=cost_font)
    bw = bb[2] - bb[0]
    bh = bb[3] - bb[1]
    bd2.text((cx - bw / 2 - 2, cy - bh / 2 - 12), str(cost), font=cost_font, fill=(255, 255, 255, 255))
    return base


def draw_glow(img, cx, cy, radius, color):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=color + (90,))
    layer = layer.filter(ImageFilter.GaussianBlur(28))
    img.alpha_composite(layer)


def draw_cannon(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 380, 150, (255, 170, 60))
    base = (160, 470, 352, 540)
    rounded_rect(d, base, 16, fill=(110, 90, 60, 255), outline=(60, 45, 25, 255), width=4)
    body = (180, 360, 332, 470)
    rounded_rect(d, body, 18, fill=(150, 122, 80, 255), outline=(80, 60, 35, 255), width=4)
    d.rectangle((196, 388, 316, 408), fill=(80, 60, 35, 255))
    d.rectangle((196, 432, 316, 452), fill=(80, 60, 35, 255))
    barrel = (236, 220, 276, 380)
    rounded_rect(d, barrel, 18, fill=(56, 56, 64, 255), outline=(28, 28, 36, 255), width=4)
    d.ellipse((220, 200, 292, 240), fill=(36, 36, 44, 255), outline=(20, 20, 28, 255), width=4)
    d.ellipse((232, 188, 280, 230), fill=(220, 110, 30, 255))
    d.ellipse((242, 180, 270, 220), fill=(255, 220, 90, 255))
    for i, dx in enumerate((-30, 30)):
        d.line((256 + dx * 0.4, 200 - 20, 256 + dx, 160 - i * 10), fill=(255, 200, 60, 255), width=4)


def draw_tesla(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 380, 180, (140, 200, 255))
    base = (170, 480, 342, 540)
    rounded_rect(d, base, 14, fill=(70, 70, 90, 255), outline=(30, 30, 50, 255), width=4)
    for i, (y0, y1, w_inset) in enumerate([(440, 480, 28), (390, 440, 38), (340, 390, 48), (290, 340, 58)]):
        rounded_rect(d, (170 + w_inset, y0, 342 - w_inset, y1), 10, fill=(110, 130, 170, 255), outline=(40, 60, 100, 255), width=3)
    coil_cx, coil_cy = 256, 270
    for r in (60, 48, 36, 24):
        d.ellipse((coil_cx - r, coil_cy - r * 0.3, coil_cx + r, coil_cy + r * 0.3), outline=(180, 220, 255, 255), width=4)
    d.ellipse((coil_cx - 18, coil_cy - 28, coil_cx + 18, coil_cy + 8), fill=(255, 255, 255, 255))
    pts = [
        [(coil_cx, 250), (236, 200), (256, 180), (220, 130)],
        [(coil_cx, 250), (276, 210), (260, 170), (300, 140)],
    ]
    for poly in pts:
        d.line(poly, fill=(180, 230, 255, 255), width=6, joint="curve")
        d.line(poly, fill=(255, 255, 255, 255), width=2, joint="curve")


def draw_totem(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 380, 160, (140, 220, 140))
    base = (170, 500, 342, 540)
    rounded_rect(d, base, 14, fill=(80, 65, 50, 255), outline=(40, 30, 20, 255), width=4)
    blocks = [
        (190, 410, 322, 500, (130, 110, 90)),
        (200, 320, 312, 410, (160, 140, 120)),
        (210, 230, 302, 320, (130, 110, 90)),
    ]
    for box in blocks:
        x0, y0, x1, y1, col = box
        rounded_rect(d, (x0, y0, x1, y1), 10, fill=col + (255,), outline=(50, 40, 30, 255), width=3)
    d.polygon([(220, 230), (256, 200), (292, 230)], fill=(160, 140, 120, 255), outline=(50, 40, 30, 255))
    d.ellipse((230, 350, 258, 380), fill=(255, 255, 255, 255))
    d.ellipse((254, 350, 282, 380), fill=(255, 255, 255, 255))
    d.ellipse((236, 358, 252, 374), fill=(40, 30, 30, 255))
    d.ellipse((260, 358, 276, 374), fill=(40, 30, 30, 255))
    d.polygon([(228, 440), (284, 440), (256, 480)], fill=(60, 30, 30, 255))
    for fang in [(244, 440, 252, 460), (260, 440, 268, 460)]:
        d.polygon([(fang[0], fang[1]), (fang[2], fang[1]), ((fang[0] + fang[2]) / 2, fang[3])], fill=(255, 255, 240, 255))


def draw_bomber(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 360, 160, (255, 180, 70))
    body = (200, 360, 320, 500)
    rounded_rect(d, body, 30, fill=(120, 180, 90, 255), outline=(60, 100, 50, 255), width=4)
    d.ellipse((210, 250, 310, 360), fill=(150, 200, 110, 255), outline=(60, 100, 50, 255), width=4)
    d.polygon([(208, 270), (220, 230), (235, 268)], fill=(150, 200, 110, 255), outline=(60, 100, 50, 255))
    d.polygon([(285, 268), (300, 230), (312, 270)], fill=(150, 200, 110, 255), outline=(60, 100, 50, 255))
    d.ellipse((232, 290, 252, 318), fill=(255, 255, 255, 255))
    d.ellipse((268, 290, 288, 318), fill=(255, 255, 255, 255))
    d.ellipse((238, 296, 250, 312), fill=(40, 30, 30, 255))
    d.ellipse((274, 296, 286, 312), fill=(40, 30, 30, 255))
    d.arc((240, 320, 280, 350), 200, 340, fill=(40, 30, 30, 255), width=3)
    d.polygon([(228, 320), (244, 312), (240, 326)], fill=(255, 255, 240, 255))
    d.polygon([(284, 320), (268, 312), (272, 326)], fill=(255, 255, 240, 255))
    bomb_cx, bomb_cy, bomb_r = 380, 280, 56
    d.ellipse((bomb_cx - bomb_r, bomb_cy - bomb_r, bomb_cx + bomb_r, bomb_cy + bomb_r), fill=(20, 20, 24, 255), outline=(60, 60, 70, 255), width=4)
    d.ellipse((bomb_cx - 30, bomb_cy - 30, bomb_cx - 10, bomb_cy - 10), fill=(120, 120, 140, 255))
    d.line((bomb_cx, bomb_cy - bomb_r, bomb_cx + 22, bomb_cy - bomb_r - 36), fill=(80, 60, 30, 255), width=5)
    d.ellipse((bomb_cx + 14, bomb_cy - bomb_r - 50, bomb_cx + 36, bomb_cy - bomb_r - 26), fill=(255, 200, 60, 255))
    d.ellipse((bomb_cx + 20, bomb_cy - bomb_r - 56, bomb_cx + 32, bomb_cy - bomb_r - 40), fill=(255, 240, 130, 255))


def draw_doge_mage(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 360, 200, (180, 130, 255))
    body = (180, 380, 332, 520)
    rounded_rect(d, body, 36, fill=(220, 175, 120, 255), outline=(120, 90, 60, 255), width=4)
    head = (200, 250, 312, 380)
    rounded_rect(d, head, 32, fill=(230, 185, 130, 255), outline=(120, 90, 60, 255), width=4)
    d.polygon([(210, 270), (200, 220), (240, 260)], fill=(230, 185, 130, 255), outline=(120, 90, 60, 255))
    d.polygon([(302, 270), (312, 220), (272, 260)], fill=(230, 185, 130, 255), outline=(120, 90, 60, 255))
    d.ellipse((222, 290, 244, 318), fill=(255, 255, 255, 255))
    d.ellipse((268, 290, 290, 318), fill=(255, 255, 255, 255))
    d.ellipse((228, 296, 240, 310), fill=(40, 30, 30, 255))
    d.ellipse((274, 296, 286, 310), fill=(40, 30, 30, 255))
    d.polygon([(244, 332), (268, 332), (256, 348)], fill=(40, 30, 30, 255))
    d.arc((232, 340, 280, 372), 210, 330, fill=(40, 30, 30, 255), width=4)
    hat_pts = [(180, 240), (332, 240), (256, 90)]
    d.polygon(hat_pts, fill=(80, 50, 140, 255), outline=(40, 25, 80, 255))
    for star_x, star_y, sr in [(225, 180, 9), (290, 200, 7), (256, 130, 8)]:
        pts = []
        for i in range(10):
            ang = math.pi / 2 + i * math.pi / 5
            r = sr if i % 2 == 0 else sr * 0.45
            pts.append((star_x + math.cos(ang) * r, star_y - math.sin(ang) * r))
        d.polygon(pts, fill=(255, 220, 80, 255))
    rounded_rect(d, (172, 232, 340, 250), 8, fill=(255, 220, 80, 255), outline=(140, 110, 30, 255), width=3)
    staff = [(330, 280), (440, 200), (430, 380), (330, 460)]
    d.line(staff[:2], fill=(120, 80, 40, 255), width=8)
    d.line(staff[1:3], fill=(120, 80, 40, 255), width=8)
    orb_cx, orb_cy = 440, 200
    for r, alpha in [(40, 60), (28, 110), (18, 200)]:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        ld.ellipse((orb_cx - r, orb_cy - r, orb_cx + r, orb_cy + r), fill=(180, 130, 255, alpha))
        layer = layer.filter(ImageFilter.GaussianBlur(6))
        img.alpha_composite(layer)
    d.ellipse((orb_cx - 12, orb_cy - 12, orb_cx + 12, orb_cy + 12), fill=(255, 255, 255, 255))


def draw_imposter(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 360, 170, (255, 80, 80))
    # Body — classic Among Us suit (red)
    body = [(200, 320), (200, 480), (220, 540), (292, 540), (312, 480), (312, 350)]
    d.polygon(body, fill=(190, 30, 30, 255), outline=(70, 10, 10, 255))
    # Helmet
    d.ellipse((196, 240, 316, 380), fill=(190, 30, 30, 255), outline=(70, 10, 10, 255), width=4)
    # Visor — light cyan glass
    d.ellipse((216, 280, 316, 350), fill=(140, 220, 240, 255), outline=(40, 80, 110, 255), width=4)
    d.ellipse((250, 290, 308, 320), fill=(220, 245, 255, 255))
    # Backpack
    rounded_rect(d, (172, 360, 200, 460), 8, fill=(150, 20, 20, 255), outline=(60, 10, 10, 255), width=3)
    # Knife
    d.polygon([(316, 360), (380, 320), (390, 340), (326, 384)], fill=(220, 220, 230, 255), outline=(120, 120, 130, 255))
    rounded_rect(d, (320, 380, 400, 396), 4, fill=(80, 50, 30, 255), outline=(40, 25, 15, 255), width=2)
    # "sus" label
    f = find_font(40)
    d.text((226, 552), "sus", fill=(255, 230, 90, 255), font=f, stroke_width=3, stroke_fill=(40, 0, 0, 255))


def draw_imposter_hut(img):
    d = ImageDraw.Draw(img)
    draw_glow(img, W / 2, 380, 170, (255, 100, 100))
    # Floor pad
    rounded_rect(d, (140, 500, 372, 545), 10, fill=(70, 70, 80, 255), outline=(30, 30, 40, 255), width=3)
    # Hut body — red sus
    rounded_rect(d, (170, 320, 342, 510), 22, fill=(190, 30, 30, 255), outline=(70, 10, 10, 255), width=4)
    # Roof — dome
    d.ellipse((180, 270, 332, 360), fill=(140, 18, 18, 255), outline=(60, 8, 8, 255), width=4)
    # Porthole
    d.ellipse((216, 360, 296, 440), fill=(140, 220, 240, 255), outline=(40, 80, 110, 255), width=5)
    d.ellipse((232, 376, 280, 414), fill=(220, 245, 255, 255))
    # Door
    rounded_rect(d, (244, 450, 268, 510), 6, fill=(70, 18, 18, 255), outline=(30, 5, 5, 255), width=3)
    # Antennae
    d.line((300, 270, 320, 200), fill=(40, 40, 50, 255), width=6)
    d.ellipse((312, 188, 332, 208), fill=(255, 220, 80, 255), outline=(140, 110, 20, 255), width=2)
    d.line((212, 270, 198, 220), fill=(40, 40, 50, 255), width=6)
    d.ellipse((188, 208, 208, 228), fill=(255, 100, 100, 255), outline=(140, 30, 30, 255), width=2)
    # Sus label on door
    f = find_font(28)
    d.text((212, 540), "хижина sus", fill=(255, 230, 90, 255), font=f, stroke_width=2, stroke_fill=(40, 0, 0, 255))


DRAWERS = {
    "cannon": draw_cannon,
    "tesla": draw_tesla,
    "totem": draw_totem,
    "bomber": draw_bomber,
    "doge_mage": draw_doge_mage,
    "imposter": draw_imposter,
    "imposter_hut": draw_imposter_hut,
}


def make_card(card):
    img = draw_frame(card)
    draw_fn = DRAWERS.get(card["id"])
    if draw_fn:
        draw_fn(img)
    return img


def main():
    data = json.load(open(CARDS_JSON, encoding="utf-8"))
    by_id = {c["id"]: c for c in data["cards"]}
    targets = ["cannon", "tesla", "totem", "bomber", "doge_mage", "imposter", "imposter_hut"]
    for cid in targets:
        if cid not in by_id:
            print("missing card metadata for", cid)
            continue
        img = make_card(by_id[cid])
        out = os.path.join(OUT_DIR, cid + ".png")
        img.save(out)
        print("wrote", out)


if __name__ == "__main__":
    main()
