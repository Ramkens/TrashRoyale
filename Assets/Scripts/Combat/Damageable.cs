using UnityEngine;
using TrashRoyale.Match;

namespace TrashRoyale.Combat
{
    public abstract class Damageable : MonoBehaviour
    {
        public Team team;
        public float maxHp = 100f;
        public float hp = 100f;
        public bool isBuilding = false;
        public bool isAir = false;
        public bool isDead = false;
        public Transform aimPoint;

        public virtual void Init(Team t, float maxHpValue)
        {
            team = t;
            maxHp = Mathf.Max(1f, maxHpValue);
            hp = maxHp;
            isDead = false;
        }

        public virtual void TakeDamage(float dmg, Damageable source = null)
        {
            if (isDead) return;
            hp -= dmg;
            if (hp <= 0f)
            {
                hp = 0f;
                isDead = true;
                OnDeath();
            }
        }

        protected abstract void OnDeath();

        public Vector3 AimPos => aimPoint != null ? aimPoint.position : transform.position + Vector3.up * 0.5f;
    }
}
