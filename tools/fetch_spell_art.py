"""
Pull spell card-art from clash-royale.fandom.com (RU) and write to
TrashRoyale/tools/art_raw/{spell_id}.png so gen_card_art.py composites
them into the proper purple+gold card frame on the next regen.

Strategy: for each spell, hit the MediaWiki action=parse API to get
all <img> on the page. Pick the first .png that looks like a card
portrait (largest "image" with reasonable dimensions, hosted on
static.wikia.nocookie.net). Strip the "/revision/..." suffix so we
download the source-resolution image.
"""
import io
import os
import re
import sys
import urllib.parse
import urllib.request

ROOT = "/home/ubuntu/repos/TrashRoyale"
OUT_DIR = os.path.join(ROOT, "tools/art_raw")
os.makedirs(OUT_DIR, exist_ok=True)

# id -> (Russian wiki page title, English fallback page title)
# The EN wiki (clashroyale.fandom.com) hosts more reliable card-render
# PNGs than the RU mirror, so we always fall back to EN; some spells
# only exist in the EN wiki by their English name.
SPELLS = {
    "fireball": ("Огненный_шар", "Fireball"),
    "freeze_spell": ("Заморозка", "Freeze"),
    "lightning_spell": ("Молния", "Lightning"),
    "rage_spell": ("Ярость", "Rage"),
    "mom_toy": ("Бочка_варваров", "Barbarian_Barrel"),
}

# Direct card-render URLs as a last-resort fallback. These are the
# stable static.wikia paths that are hot-linked elsewhere; if both
# the RU and EN MediaWiki APIs return 404'd image links we drop down
# to these. Verified manually 2025-11.
DIRECT_FALLBACKS = {
    "fireball": "https://static.wikia.nocookie.net/clashroyale/images/4/4f/Fireball_card.png",
    "freeze_spell": "https://static.wikia.nocookie.net/clashroyale/images/d/dc/Freeze_card.png",
    "lightning_spell": "https://static.wikia.nocookie.net/clashroyale/images/3/3d/Lightning_card.png",
    "rage_spell": "https://static.wikia.nocookie.net/clashroyale/images/4/4d/Rage_card.png",
    "mom_toy": "https://static.wikia.nocookie.net/clashroyale/images/5/54/Barbarian_Barrel_card_render.png",
}

USER_AGENT = "Mozilla/5.0 (TrashRoyaleArtFetch/1.0)"


def fetch(url):
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.read()


def get_card_image(page_title, lang):
    """Use the parse API to enumerate images on the page, pick the
    page's lead image (the .png whose name matches the title)."""
    api = (
        f"https://clash-royale.fandom.com/{lang}/api.php"
        f"?action=parse&page={urllib.parse.quote(page_title)}&format=json&prop=images"
    )
    try:
        import json as _j
        raw = fetch(api).decode("utf-8", errors="ignore")
        data = _j.loads(raw)
    except Exception as e:
        print(f"  api fail: {e}", file=sys.stderr)
        return None
    images = (data.get("parse") or {}).get("images") or []
    if not images:
        return None
    # Prefer images whose name contains the page title (or its English
    # equivalent); CR fandom uniformly names these like
    # "Огненный_шар.png" or "FireballCard.png".
    title_token = page_title.replace(" ", "_").lower()
    candidates = []
    for img in images:
        low = img.lower()
        if not low.endswith((".png", ".webp", ".jpg", ".jpeg")):
            continue
        score = 0
        if title_token in low:
            score += 5
        if "card" in low:
            score += 3
        if low.startswith("emote_") or "icon" in low or "emoji" in low:
            score -= 5
        candidates.append((score, img))
    candidates.sort(reverse=True)
    if not candidates:
        return None
    file_name = candidates[0][1]
    # Resolve to the actual file URL via API.
    info_api = (
        f"https://clash-royale.fandom.com/{lang}/api.php"
        f"?action=query&titles=File:{urllib.parse.quote(file_name)}"
        f"&prop=imageinfo&iiprop=url&format=json"
    )
    try:
        import json as _j
        raw = fetch(info_api).decode("utf-8", errors="ignore")
        data = _j.loads(raw)
    except Exception as e:
        print(f"  imageinfo fail: {e}", file=sys.stderr)
        return None
    pages = (data.get("query") or {}).get("pages") or {}
    for _pid, p in pages.items():
        infos = p.get("imageinfo") or []
        if infos:
            url = infos[0].get("url")
            # Strip "/revision/.../" segment so we get the source size.
            url = re.sub(r"/revision/.*", "", url)
            return url
    return None


def try_download(url):
    try:
        return fetch(url)
    except Exception as e:
        print(f"  download fail: {e}")
        return None


def fetch_one(card_id, ru_title, en_title):
    print(f"[{card_id}] trying RU: {ru_title}")
    url = get_card_image(ru_title, "ru")
    blob = try_download(url) if url else None
    if not blob:
        print(f"  RU miss; trying EN: {en_title}")
        url = get_card_image(en_title, "")  # global wiki uses no lang prefix
        blob = try_download(url) if url else None
    if not blob:
        url = DIRECT_FALLBACKS.get(card_id)
        if url:
            print(f"  EN miss; using direct fallback: {url}")
            blob = try_download(url)
    if not blob:
        print(f"  -> NO IMAGE FOUND")
        return False
    print(f"  -> {url}")
    out = os.path.join(OUT_DIR, f"{card_id}.png")
    # Convert webp -> png if needed; otherwise dump bytes.
    if url.lower().endswith(".webp"):
        try:
            from PIL import Image
            img = Image.open(io.BytesIO(blob)).convert("RGBA")
            img.save(out)
        except Exception as e:
            print(f"  webp-convert fail: {e}; saving raw bytes")
            with open(out, "wb") as f:
                f.write(blob)
    else:
        with open(out, "wb") as f:
            f.write(blob)
    print(f"  saved -> {out} ({len(blob)} bytes)")
    return True


if __name__ == "__main__":
    ok = fail = 0
    for cid, (ru, en) in SPELLS.items():
        if fetch_one(cid, ru, en):
            ok += 1
        else:
            fail += 1
    print(f"\n{ok} ok, {fail} failed")
    sys.exit(0 if fail == 0 else 1)
