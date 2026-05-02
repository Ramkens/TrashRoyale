using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrashRoyale.Core;
using TrashRoyale.Persistence;

namespace TrashRoyale.UI
{
    public class DeckEditor : MonoBehaviour
    {
        PlayerProfile _profile;
        Transform _slotsRoot;
        Transform _gridRoot;
        Image[] _slotImages = new Image[8];
        TMP_Text[] _slotNames = new TMP_Text[8];

        public static DeckEditor Open(Transform canvas, PlayerProfile profile)
        {
            var go = new GameObject("DeckEditor");
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.92f);

            var ed = go.AddComponent<DeckEditor>();
            ed._profile = profile;
            ed.Build();
            return ed;
        }

        void Build()
        {
            CardDatabase.EnsureLoaded();
            var title = UIFactory.MakeText(transform, "Title", "Колода", 70, TextAlignmentOptions.Center);
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 0.98f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            title.color = new Color(1f, 0.9f, 0.3f);

            var slotsPanel = UIFactory.MakePanel(transform, "Slots", new Color(0.1f, 0.1f, 0.2f, 0.9f));
            var sprt = slotsPanel.GetComponent<RectTransform>();
            sprt.anchorMin = new Vector2(0.05f, 0.7f);
            sprt.anchorMax = new Vector2(0.95f, 0.91f);
            sprt.offsetMin = sprt.offsetMax = Vector2.zero;
            _slotsRoot = slotsPanel.transform;

            for (int i = 0; i < 8; i++)
            {
                var card = i < _profile.deck.Count ? CardDatabase.Get(_profile.deck[i]) : null;
                int idx = i;
                var slot = MakeSlotChip(_slotsRoot, i, card, () => RemoveAtSlot(idx));
                _slotImages[i] = slot.GetComponent<Image>();
                _slotNames[i] = slot.transform.GetChild(0).GetComponent<TMP_Text>();
            }

            var libPanel = UIFactory.MakePanel(transform, "Library", new Color(0.05f, 0.05f, 0.1f, 0.95f));
            var lrt = libPanel.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.05f, 0.13f);
            lrt.anchorMax = new Vector2(0.95f, 0.68f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            _gridRoot = libPanel.transform;

            int row = 0, col = 0;
            float cellW = 280, cellH = 180, gap = 14;
            int columns = 3;
            int idx2 = 0;
            foreach (var c in CardDatabase.All)
            {
                CardData captured = c;
                var chip = MakeLibraryChip(_gridRoot, c, () => AddCard(captured.id));
                var crt = chip.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(0, 1);
                crt.pivot = new Vector2(0, 1);
                crt.sizeDelta = new Vector2(cellW, cellH);
                crt.anchoredPosition = new Vector2(20 + col * (cellW + gap), -20 - row * (cellH + gap));
                col++;
                if (col >= columns) { col = 0; row++; }
                idx2++;
            }

            var save = UIFactory.MakeButton(transform, "Save", "Сохранить", () =>
            {
                _profile.Save();
                Destroy(gameObject);
            });
            var srt2 = save.GetComponent<RectTransform>();
            srt2.anchorMin = new Vector2(0.18f, 0.04f);
            srt2.anchorMax = new Vector2(0.5f, 0.11f);
            srt2.offsetMin = srt2.offsetMax = Vector2.zero;
            save.image.color = new Color(0.3f, 0.8f, 0.3f);

            var close = UIFactory.MakeButton(transform, "Close", "Закрыть", () => Destroy(gameObject));
            var crt2 = close.GetComponent<RectTransform>();
            crt2.anchorMin = new Vector2(0.5f, 0.04f);
            crt2.anchorMax = new Vector2(0.82f, 0.11f);
            crt2.offsetMin = crt2.offsetMax = Vector2.zero;
            close.image.color = new Color(0.7f, 0.3f, 0.3f);
        }

        GameObject MakeSlotChip(Transform parent, int slotIdx, CardData c, System.Action onClick)
        {
            var go = new GameObject($"Slot_{slotIdx}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            float w = 200f, h = 130f, gap = 10f;
            float total = w * 8 + gap * 7;
            float startX = -total / 2 + w / 2;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(startX + slotIdx * (w + gap), 0);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.85f, 0.7f, 0.4f);

            var t = UIFactory.MakeText(go.transform, "Name", c != null ? $"{c.displayName}\n{c.elixirCost}⚡" : "Пусто", 26, TextAlignmentOptions.Center);
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return go;
        }

        GameObject MakeLibraryChip(Transform parent, CardData c, System.Action onClick)
        {
            var go = new GameObject("Lib_" + c.id);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = _profile.deck.Contains(c.id) ? new Color(0.4f, 0.4f, 0.5f) : new Color(0.85f, 0.7f, 0.4f);
            var t = UIFactory.MakeText(go.transform, "Name", $"{c.displayName}\n{c.elixirCost}⚡ • {c.kind}", 22, TextAlignmentOptions.Center);
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return go;
        }

        void RemoveAtSlot(int slot)
        {
            if (slot < 0 || slot >= _profile.deck.Count) return;
            _profile.deck.RemoveAt(slot);
            Refresh();
        }

        void AddCard(string id)
        {
            if (_profile.deck.Count >= 8) return;
            if (_profile.deck.Contains(id)) return;
            _profile.deck.Add(id);
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < 8; i++)
            {
                var c = i < _profile.deck.Count ? CardDatabase.Get(_profile.deck[i]) : null;
                _slotNames[i].text = c != null ? $"{c.displayName}\n{c.elixirCost}⚡" : "Пусто";
            }
            Destroy(gameObject);
            DeckEditor.Open(transform.parent, _profile);
        }
    }
}
