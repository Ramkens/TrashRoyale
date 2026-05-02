using UnityEngine;
using TrashRoyale.Util;

namespace TrashRoyale.Combat
{
    public static class FxFactory
    {
        public static void SpawnHit(Vector3 pos)
        {
            var go = new GameObject("HitFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.3f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.9f, 0.3f);
            main.maxParticles = 20;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = SafeShader.NewSpriteMaterial();
        }

        public static void SpawnPoof(Vector3 pos)
        {
            var go = new GameObject("PoofFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.6f;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.5f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.9f, 0.85f, 0.7f, 0.8f);
            main.maxParticles = 30;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = SafeShader.NewSpriteMaterial();
        }

        public static void SpawnExplosion(Vector3 pos, float radius)
        {
            var go = new GameObject("ExplosionFx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.8f;
            main.startLifetime = 0.7f;
            main.startSpeed = radius * 3.5f;
            main.startSize = radius * 0.5f;
            main.startColor = new Color(1f, 0.4f, 0.05f);
            main.maxParticles = 100;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.2f;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.material = SafeShader.NewSpriteMaterial();
        }
    }
}
