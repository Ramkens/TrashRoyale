using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Match;
using TrashRoyale.Util;

namespace TrashRoyale.UI
{
    /// <summary>
    /// World-space overlay shown while the player is dragging a card. Renders:
    /// - A red translucent quad over every "forbidden" cell of the arena so the
    ///   player can see at a glance where they can't place units.
    /// - A white ring at the cursor position showing where units will spawn (or,
    ///   for spells, a circle of <c>splashRadius</c>).
    /// The overlay is rebuilt the first time it's shown and the cells just have
    /// their materials toggled on/off as the territory unlocks.
    /// </summary>
    public class PlacementOverlay : MonoBehaviour
    {
        const int CellsX = 9;     // grid cells across the width of the arena
        const int CellsZ = 16;    // and along the length
        readonly List<MeshRenderer> _cells = new();
        readonly List<Vector3> _cellCenters = new();
        Material _redMat;
        GameObject _ring;
        Material _ringMat;
        Transform _ringT;
        // Secondary ring shown only for Building cards: previews the
        // weapon's attack-range circle so players can plan defenses.
        GameObject _rangeRing;
        Material _rangeRingMat;
        Transform _rangeRingT;
        bool _built;

        public static PlacementOverlay Create()
        {
            var go = new GameObject("PlacementOverlay");
            return go.AddComponent<PlacementOverlay>();
        }

        public void Show(CardData card)
        {
            EnsureBuilt();
            UpdateForbiddenCells(card);
            gameObject.SetActive(true);
            if (_ring != null)
            {
                // Cursor ring scale: spells use their splashRadius, units
                // use the spawnRadius (or a small floor so single-spawns
                // are still visible), buildings use the deploy footprint.
                float scale = 1.2f;
                if (card != null)
                {
                    if (card.Kind == CardKind.Spell && card.splashRadius > 0.1f)
                        scale = card.splashRadius * 2f;
                    else if (card.Kind == CardKind.Unit)
                        scale = Mathf.Max(1.0f, card.spawnRadius * 2f + 0.6f);
                    else if (card.Kind == CardKind.Building)
                        scale = 1.4f;
                }
                _ringT.localScale = new Vector3(scale, 0.02f, scale);
                _ringMat.color = card != null && card.Kind == CardKind.Spell
                    ? new Color(1f, 0.6f, 0.2f, 0.55f)
                    : new Color(1f, 1f, 1f, 0.85f);
            }
            // Building cards get an extra translucent ring at
            // 2*card.range showing where their weapon reaches. Hidden
            // for non-buildings or buildings without a weapon.
            if (_rangeRing != null)
            {
                bool show = card != null
                    && card.Kind == CardKind.Building
                    && card.range > 0.1f
                    && card.attackInterval > 0.05f
                    && card.damage > 0.1f;
                _rangeRing.SetActive(show);
                if (show)
                {
                    float diameter = card.range * 2f;
                    _rangeRingT.localScale = new Vector3(diameter, 0.02f, diameter);
                    _rangeRingMat.color = new Color(1f, 0.85f, 0.35f, 0.32f);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetCursor(Vector3 worldPos, bool valid)
        {
            if (_ring == null) return;
            var p = new Vector3(worldPos.x, 0.06f, worldPos.z);
            _ringT.position = p;
            // Tint the ring red when hovering an invalid spot so the player gets
            // immediate feedback without having to read the floor overlay.
            if (_ringMat != null)
            {
                if (!valid)
                {
                    _ringMat.color = new Color(1f, 0.25f, 0.25f, 0.85f);
                }
                else
                {
                    _ringMat.color = new Color(1f, 1f, 1f, 0.9f);
                }
            }
            // Track the building range ring at the same cursor position so
            // it feels like a single "placement preview" anchored under
            // the finger.
            if (_rangeRing != null && _rangeRing.activeSelf)
            {
                _rangeRingT.position = p;
            }
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            _redMat = SafeShader.NewTransparentMaterial(new Color(1f, 0.18f, 0.18f, 0.32f));

            float cellW = (ArenaController.HalfWidth * 2f) / CellsX;
            float cellL = (ArenaController.HalfLength * 2f) / CellsZ;
            for (int xi = 0; xi < CellsX; xi++)
            {
                for (int zi = 0; zi < CellsZ; zi++)
                {
                    float x = -ArenaController.HalfWidth + cellW * (xi + 0.5f);
                    float z = -ArenaController.HalfLength + cellL * (zi + 0.5f);
                    var cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cell.name = $"Cell_{xi}_{zi}";
                    cell.transform.SetParent(transform, false);
                    cell.transform.position = new Vector3(x, 0.03f, z);
                    cell.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    cell.transform.localScale = new Vector3(cellW * 0.94f, cellL * 0.94f, 1f);
                    Object.Destroy(cell.GetComponent<Collider>());
                    var mr = cell.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = _redMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    _cells.Add(mr);
                    _cellCenters.Add(new Vector3(x, 0f, z));
                }
            }

            // White cursor ring (cylinder works as a flat disc).
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ring.name = "CursorRing";
            _ring.transform.SetParent(transform, false);
            _ring.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            _ring.transform.localPosition = new Vector3(0, 0.06f, 0);
            Object.Destroy(_ring.GetComponent<Collider>());
            _ringMat = SafeShader.NewTransparentMaterial(new Color(1f, 1f, 1f, 0.85f));
            _ring.GetComponent<MeshRenderer>().sharedMaterial = _ringMat;
            _ringT = _ring.transform;

            // Larger amber ring used by buildings to preview attack range.
            _rangeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _rangeRing.name = "BuildingRangeRing";
            _rangeRing.transform.SetParent(transform, false);
            _rangeRing.transform.localScale = new Vector3(4f, 0.02f, 4f);
            _rangeRing.transform.localPosition = new Vector3(0, 0.05f, 0);
            Object.Destroy(_rangeRing.GetComponent<Collider>());
            _rangeRingMat = SafeShader.NewTransparentMaterial(new Color(1f, 0.85f, 0.35f, 0.32f));
            _rangeRing.GetComponent<MeshRenderer>().sharedMaterial = _rangeRingMat;
            _rangeRingT = _rangeRing.transform;
            _rangeRing.SetActive(false);
        }

        void UpdateForbiddenCells(CardData card)
        {
            var arena = ArenaController.I;
            if (arena == null)
            {
                foreach (var c in _cells) c.enabled = false;
                return;
            }
            // Spells: nothing forbidden inside arena bounds.
            if (card != null && card.Kind == CardKind.Spell)
            {
                foreach (var c in _cells) c.enabled = false;
                return;
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                var pos = _cellCenters[i];
                bool ok = arena.IsValidPlacement(Team.Player, pos, card);
                _cells[i].enabled = !ok;
            }
        }
    }
}
