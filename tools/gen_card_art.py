#!/usr/bin/env python3
"""
Generate Clash Royale-style card images:
- Gold-framed rectangle with elixir cost bubble (top-left)
- Card art in middle (use Sketchfab thumbnail as base)
- Card name banner (bottom)
- Output PNG sized for Unity import (512x768)
"""

import io
import os
import json
import sys
import urllib.request
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
CARDS_JSON = ROOT / "Assets/Resources/Cards/cards.json"
OUT = ROOT / "Assets/Resources/CardArt"
OUT.mkdir(parents=True, exist_ok=True)

SKETCHFAB_TOKEN = os.environ.get("SKETCHFAB_API_TOKEN", "")

# Maps card.id to a Sketchfab model UID for thumbnail fetch
SF_THUMBS = {
    "knight":             "0722db29739c4a278651769c3ec3d05e",
    "pig":                "26ae14bd0d2b4650b4bf878ca85ad06a",
    "skibidi":            "62ea2ffb7d37476e8e5f3e93c2ef5aea",
    "pocoyo":             "16c09e971fe7494790b2f5daa5e065e5",
    "amongus":            "428bb9a3637e458c8336e4a7aefd4e3d",
    "cheems":             "912a6ee6504b4b7a8b0226000e01cdea",
    "shrek":              "ff6a111c58c94d328b0880a38b912428",
    "gigachad":           "405a54167dfc439d937973bab248ea26",
    "nyancat":            "7841f5567aa34453b596e81e12a76e45",
}

# Local image overrides (take priority over Sketchfab)
LOCAL_ART = {
    "fireball": str(ROOT / "_assets/fireball_user.png"),
}

CARD_W, CARD_H = 512, 768
ART_PAD = 24
ART_TOP = 96
ART_BOTTOM = 200
NAME_BG_COLOR = (60, 30, 130, 235)
FRAME_COLOR = (220, 180, 60)
INNER_BG_TOP = (46, 60, 120)
INNER_BG_BOTTOM = (24, 28, 60)
ELIXIR_OUTER = (180, 50, 200)
ELIXIR_INNER = (245, 100, 230)


def font(size):
    candidates = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf",
    ]
    for c in candidates:
        if os.path.exists(c):
            return ImageFont.truetype(c, size)
    return ImageFont.load_default()


def fetch_thumb(uid):
    if not SKETCHFAB_TOKEN:
        return None
    try:
        req = urllib.request.Request(
            f"https://api.sketchfab.com/v3/models/{uid}",
            headers={"Authorization": f"Token {SKETCHFAB_TOKEN}"},
        )
        with urllib.request.urlopen(req, timeout=20) as r:
            data = json.loads(r.read().decode("utf-8"))
        thumbs = data.get("thumbnails", {}).get("images", [])
        if not thumbs:
            return None
        thumbs = sorted(thumbs, key=lambda x: -x.get("width", 0))
        url = thumbs[0]["url"]
        with urllib.request.urlopen(url, timeout=20) as r:
            return Image.open(io.BytesIO(r.read())).convert("RGBA")
    except Exception as e:
        print(f"  thumb fetch failed: {e}")
        return None


def gradient_fill(width, height, top_color, bottom_color):
    img = Image.new("RGBA", (width, height))
    px = img.load()
    for y in range(height):
        t = y / max(1, height - 1)
        r = int(top_color[0] * (1 - t) + bottom_color[0] * t)
        g = int(top_color[1] * (1 - t) + bottom_color[1] * t)
        b = int(top_color[2] * (1 - t) + bottom_color[2] * t)
        for x in range(width):
            px[x, y] = (r, g, b, 255)
    return img


def round_rect_mask(size, radius):
    mask = Image.new("L", size, 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius=radius, fill=255)
    return mask


def draw_card(card):
    cid = card["id"]
    name = card["displayName"]
    cost = card["elixirCost"]
    desc = card.get("description", "")

    # Base card
    card_img = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))

    # Outer rounded frame
    bg = gradient_fill(CARD_W, CARD_H, INNER_BG_TOP, INNER_BG_BOTTOM)
    bg = Image.composite(bg, Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0)),
                         round_rect_mask((CARD_W, CARD_H), 48))
    card_img.alpha_composite(bg)

    # Gold frame border
    frame = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    fd = ImageDraw.Draw(frame)
    fd.rounded_rectangle((6, 6, CARD_W - 7, CARD_H - 7), radius=44, outline=FRAME_COLOR, width=10)
    fd.rounded_rectangle((22, 22, CARD_W - 23, CARD_H - 23), radius=32, outline=(255, 230, 100), width=3)
    card_img.alpha_composite(frame)

    # Art region
    art_w = CARD_W - ART_PAD * 2
    art_h = CARD_H - ART_TOP - ART_BOTTOM
    art_box = Image.new("RGBA", (art_w, art_h), (255, 255, 255, 255))
    thumb = None
    local = LOCAL_ART.get(cid)
    if local and os.path.exists(local):
        try:
            thumb = Image.open(local).convert("RGBA")
        except Exception as e:
            print(f"  local art load failed: {e}")
    if thumb is None:
        uid = SF_THUMBS.get(cid)
        if uid:
            thumb = fetch_thumb(uid)
    if thumb is None:
        # Procedural fallback fill
        proc = gradient_fill(art_w, art_h, (90, 130, 200), (30, 60, 130))
        art_box.paste(proc, (0, 0))
        d2 = ImageDraw.Draw(art_box)
        glyph = {
            "knight": "⚔",
            "pig": "🐷",
            "skibidi": "📺",
            "pocoyo": "🎉",
            "amongus": "🚀",
            "cheems": "🐶",
            "shrek": "🌳",
            "gigachad": "💪",
            "nyancat": "🌈",
            "fireball": "🔥",
        }.get(cid, "?")
        d2.text((art_w // 2, art_h // 2), glyph, anchor="mm", fill=(255, 255, 255), font=font(220))
    else:
        # Resize keeping aspect, cover the art rect
        ratio = max(art_w / thumb.width, art_h / thumb.height)
        nw = int(thumb.width * ratio)
        nh = int(thumb.height * ratio)
        thumb = thumb.resize((nw, nh), Image.LANCZOS)
        cx = (nw - art_w) // 2
        cy = (nh - art_h) // 2
        thumb = thumb.crop((cx, cy, cx + art_w, cy + art_h))
        art_box = thumb.convert("RGBA")

    art_masked = Image.composite(art_box,
                                 Image.new("RGBA", (art_w, art_h), (0, 0, 0, 0)),
                                 round_rect_mask((art_w, art_h), 24))
    card_img.alpha_composite(art_masked, (ART_PAD, ART_TOP))

    # Inner border on art
    di = ImageDraw.Draw(card_img)
    di.rounded_rectangle((ART_PAD - 2, ART_TOP - 2, ART_PAD + art_w + 1, ART_TOP + art_h + 1),
                         radius=24, outline=FRAME_COLOR, width=4)

    # Name banner
    banner_h = 90
    banner_y = CARD_H - ART_BOTTOM + 20
    di.rounded_rectangle((30, banner_y, CARD_W - 30, banner_y + banner_h),
                         radius=20, fill=NAME_BG_COLOR, outline=FRAME_COLOR, width=4)
    f_name = font(50)
    # adjust if too long
    while di.textlength(name, font=f_name) > CARD_W - 100 and f_name.size > 20:
        f_name = font(f_name.size - 4)
    di.text((CARD_W // 2, banner_y + banner_h // 2), name,
            anchor="mm", fill=(255, 235, 150), font=f_name)

    # Description (small)
    f_desc = font(20)
    desc_y = banner_y + banner_h + 16
    wrap_lines = wrap_text(desc, f_desc, CARD_W - 60, di)
    for i, line in enumerate(wrap_lines[:3]):
        di.text((CARD_W // 2, desc_y + i * 22), line, anchor="mm",
                fill=(220, 220, 250), font=f_desc)

    # Elixir bubble (top-left)
    bubble_d = 130
    bx, by = -10, -10
    bd_layer = Image.new("RGBA", (CARD_W, CARD_H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(bd_layer)
    bd.ellipse((bx, by, bx + bubble_d, by + bubble_d), fill=ELIXIR_OUTER, outline=FRAME_COLOR, width=6)
    bd.ellipse((bx + 18, by + 18, bx + bubble_d - 18, by + bubble_d - 18), fill=ELIXIR_INNER)
    f_cost = font(70)
    bd.text((bx + bubble_d // 2, by + bubble_d // 2), str(cost), anchor="mm", fill="white", font=f_cost)
    card_img.alpha_composite(bd_layer)

    return card_img


def wrap_text(text, fnt, max_w, draw):
    words = (text or "").split()
    lines = []
    cur = ""
    for w in words:
        trial = (cur + " " + w).strip()
        if draw.textlength(trial, font=fnt) <= max_w:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def main():
    with open(CARDS_JSON, "r", encoding="utf-8") as f:
        data = json.load(f)
    for card in data["cards"]:
        cid = card["id"]
        print(f"Generating card art for {cid} ({card['displayName']})...")
        img = draw_card(card)
        out_path = OUT / f"{cid}.png"
        img.save(out_path, "PNG")
        print(f"  -> {out_path}")
    print("Done.")


if __name__ == "__main__":
    sys.exit(main() or 0)
