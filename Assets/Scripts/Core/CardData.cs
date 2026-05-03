using System;
using UnityEngine;

namespace TrashRoyale.Core
{
    public enum CardKind { Unit, Spell, Building }
    public enum TargetMode { Any, BuildingsOnly }

    [Serializable]
    public class CardData
    {
        public string id;
        public string displayName;
        public string description;
        public int elixirCost;
        public string kind;
        public float hp;
        public float damage;
        public float attackInterval;
        public float range;
        public float moveSpeed;
        public float deployTime;
        public int spawnCount;
        public float spawnRadius;
        public bool isAir;
        public string targetMode;
        public bool targetsAir;
        public float splashRadius;
        public string modelKey;
        public float modelScale;
        public string voiceLine;
        // Lifetime in seconds for Building cards (cannons / teslas decay).
        // Ignored for Unit and Spell kinds. <= 0 means "permanent" (rare).
        public float lifetime;
        // Optional per-card sfx volume scale [0..1] applied on top of
        // the global SfxVolume when playing this card's voice line. Use
        // for units whose source clip is too loud (e.g. imposter shhh).
        // 0 (default) means "no override" -> uses the global volume.
        public float sfxVolume;
        // Optional per-card glTF rotation correction (degrees, applied
        // pre-bounds-measurement so auto-fit still works). Designer-
        // tunable so we can dial in Sketchfab models without editing
        // ModelLoader.OrientationOverride. 0/0/0 means no override.
        public float modelEulerX;
        public float modelEulerY;
        public float modelEulerZ;
        // Spawner-on-tick: while alive, a Building card with these set
        // emits one unit (with id `spawnOnTickCardId`) every
        // `spawnOnTickInterval` seconds. Used by Imposter Hut, Furnace,
        // etc. Ignored for Unit and Spell kinds.
        public string spawnOnTickCardId;
        public float spawnOnTickInterval;
        // Optional projectile sprite key (Resources/Projectiles/<key>.png)
        // — overrides the default "stretched capsule" projectile so cards
        // like Дуров can throw a Telegram envelope, Pepe a frog meme, etc.
        public string projectileSprite;
        // Combat behavior tags. Each one toggles a discrete mechanic on
        // the spawned Unit; multiple tags compose. See `Unit.cs` for
        // implementations. Empty list -> stock behavior.
        // Recognized values: "charge", "spawn_on_death", "berserker",
        // "lifesteal", "chain_lightning", "phase_immune", "mirror",
        // "freeze_projectile", "sniper", "multishot_3", "reflect",
        // "aura_atk_speed", "inferno_ramp".
        public string[] mechanics;
        // Tag-specific parameters; kept loose so we don't bloat the
        // schema with one knob per mechanic.
        public float chargeMultiplier;        // first hit dmg ×N after charging
        public float chargeBuildSeconds;      // moving for N seconds enables charge
        public string spawnOnDeathCardId;     // id spawned on death
        public int    spawnOnDeathCount;
        public float lifestealFraction;       // [0..1]
        public int    chainLightningJumps;     // extra targets after primary
        public float chainLightningRadius;
        public float phaseImmuneSeconds;
        public float freezeStunSeconds;
        public float multishotSpreadDeg;
        public float reflectFraction;         // [0..1] of received dmg returned
        public float auraRadius;
        public float auraAtkSpeedBonus;       // +N% atk speed for allies
        public float infernoRampStartDmg;
        public float infernoRampMaxDmg;
        public float infernoRampSeconds;

        public CardKind Kind
        {
            get
            {
                if (kind == "Spell") return CardKind.Spell;
                if (kind == "Building") return CardKind.Building;
                return CardKind.Unit;
            }
        }
        public TargetMode Targets => targetMode == "BuildingsOnly" ? TargetMode.BuildingsOnly : TargetMode.Any;
    }

    [Serializable]
    public class CardCollection
    {
        public CardData[] cards;
    }
}
