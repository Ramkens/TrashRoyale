using UnityEngine;
using TrashRoyale.Combat;
using TrashRoyale.Core;

namespace TrashRoyale.Match
{
    public class ArenaController : MonoBehaviour
    {
        public static ArenaController I { get; private set; }
        public const float HalfWidth = 4.5f;
        public const float HalfLength = 8.0f;
        public const float RiverHalfThickness = 0.5f;

        public Transform PlayerLeftBridge, PlayerRightBridge;
        public Transform[] PlayerSpawnPoints;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        public bool IsValidPlacement(Team team, Vector3 worldPos, CardData card)
        {
            if (card.Kind == CardKind.Spell) return true;
            if (Mathf.Abs(worldPos.x) > HalfWidth) return false;
            if (Mathf.Abs(worldPos.z) > HalfLength) return false;
            if (Mathf.Abs(worldPos.z) < RiverHalfThickness) return false;
            if (team == Team.Player && worldPos.z > 0f) return false;
            if (team == Team.Enemy && worldPos.z < 0f) return false;
            return true;
        }
    }
}
