using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Combat;
using TrashRoyale.Util;

namespace TrashRoyale.Match
{
    public static class ArenaBuilder
    {
        public static void Build(MatchManager match, ArenaController arena)
        {
            BuildBackdrop();
            BuildGround();
            BuildRiver();
            BuildBridges();
            BuildTowers(match);
            ConfigureCamera();
            ConfigureLight();
        }

        static Material LitMat(Color c, Texture2D tex = null, Vector2? tile = null)
        {
            var m = new Material(SafeShader.Opaque);
            m.color = c;
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (tile.HasValue)
                {
                    if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tile.Value);
                    if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tile.Value);
                }
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        static Texture2D Tex(string path) { return Resources.Load<Texture2D>(path); }

        static void BuildBackdrop()
        {
            // Big sky/clouds plane behind arena
            var bgPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgPlane.name = "Backdrop";
            bgPlane.transform.position = new Vector3(0, 6f, ArenaController.HalfLength + 14f);
            bgPlane.transform.localScale = new Vector3(40f, 22f, 1f);
            bgPlane.transform.rotation = Quaternion.Euler(0, 0, 0);
            var bgMat = new Material(SafeShader.Opaque);
            bgMat.mainTexture = Tex("UI/menu_bg");
            bgMat.color = new Color(0.7f, 0.7f, 0.85f);
            bgPlane.GetComponent<MeshRenderer>().sharedMaterial = bgMat;
            Object.Destroy(bgPlane.GetComponent<Collider>());
        }

        static void BuildGround()
        {
            var grass = Tex("UI/arena_grass");
            var ground = new GameObject("Ground");
            for (int side = 0; side < 2; side++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quad.transform.SetParent(ground.transform, false);
                quad.name = side == 0 ? "PlayerSide" : "EnemySide";
                quad.transform.localScale = new Vector3(ArenaController.HalfWidth * 2f, 0.2f, ArenaController.HalfLength);
                quad.transform.position = new Vector3(0, -0.1f, side == 0 ? -ArenaController.HalfLength * 0.5f : ArenaController.HalfLength * 0.5f);
                Color tint = side == 0 ? new Color(0.85f, 1.0f, 0.85f) : new Color(0.95f, 0.85f, 1.0f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = LitMat(tint, grass, new Vector2(4, 4));
            }
        }

        static void BuildRiver()
        {
            var riverTex = Tex("UI/arena_river");
            var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river.name = "River";
            river.transform.localScale = new Vector3(ArenaController.HalfWidth * 2f, 0.05f, ArenaController.RiverHalfThickness * 2f);
            river.transform.position = new Vector3(0, -0.05f, 0);
            river.GetComponent<MeshRenderer>().sharedMaterial = LitMat(Color.white, riverTex, new Vector2(2, 1));
        }

        static void BuildBridges()
        {
            var bridgeTex = Tex("UI/arena_bridge");
            for (int side = -1; side <= 1; side += 2)
            {
                var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Bridge";
                bridge.transform.localScale = new Vector3(1.6f, 0.08f, 1.4f);
                bridge.transform.position = new Vector3(side * 2.8f, 0.0f, 0f);
                bridge.GetComponent<MeshRenderer>().sharedMaterial = LitMat(Color.white, bridgeTex, new Vector2(1, 1));
            }
        }

        static void BuildTowers(MatchManager match)
        {
            match.PlayerSideTowers.Clear();
            match.EnemySideTowers.Clear();
            for (int side = -1; side <= 1; side += 2)
            {
                var pTower = MakeTower($"PlayerTower_{(side == -1 ? "L" : "R")}", new Vector3(side * 2.8f, 0f, -ArenaController.HalfLength + 1.2f), Team.Player, false);
                var eTower = MakeTower($"EnemyTower_{(side == -1 ? "L" : "R")}", new Vector3(side * 2.8f, 0f, ArenaController.HalfLength - 1.2f), Team.Enemy, false);
                match.PlayerSideTowers.Add(pTower);
                match.EnemySideTowers.Add(eTower);
            }
            match.PlayerKing = MakeTower("PlayerKing", new Vector3(0f, 0f, -ArenaController.HalfLength + 0.4f), Team.Player, true);
            match.EnemyKing = MakeTower("EnemyKing", new Vector3(0f, 0f, ArenaController.HalfLength - 0.4f), Team.Enemy, true);
        }

        static Tower MakeTower(string name, Vector3 pos, Team team, bool king)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var brick = Tex("UI/tower_brick");
            var crown = Tex("UI/tower_king");

            // Tower base
            var baseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseB.transform.SetParent(go.transform, false);
            baseB.transform.localPosition = new Vector3(0, king ? 0.5f : 0.4f, 0);
            baseB.transform.localScale = new Vector3(king ? 1.8f : 1.25f, king ? 1.0f : 0.8f, king ? 1.8f : 1.25f);
            baseB.GetComponent<MeshRenderer>().sharedMaterial = LitMat(team == Team.Player ? new Color(0.85f, 0.95f, 1f) : new Color(1f, 0.9f, 0.9f), brick, new Vector2(2, 1));

            // Tower body
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, king ? 1.6f : 1.2f, 0);
            visual.transform.localScale = new Vector3(king ? 1.4f : 0.95f, king ? 1.6f : 1.2f, king ? 1.4f : 0.95f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = LitMat(team == Team.Player ? new Color(0.55f, 0.75f, 1f) : new Color(1f, 0.55f, 0.55f), brick, new Vector2(2, 2));

            // Roof
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(go.transform, false);
            roof.transform.localPosition = new Vector3(0, king ? 2.6f : 1.95f, 0);
            roof.transform.localScale = new Vector3(king ? 1.6f : 1.1f, 0.25f, king ? 1.6f : 1.1f);
            var roofMat = LitMat(team == Team.Player ? new Color(0.4f, 0.55f, 1f) : new Color(0.95f, 0.45f, 0.45f), king ? crown : null, new Vector2(1, 1));
            roof.GetComponent<MeshRenderer>().sharedMaterial = roofMat;

            // King flag pole
            if (king)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pole.transform.SetParent(go.transform, false);
                pole.transform.localPosition = new Vector3(0, 3.0f, 0);
                pole.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
                pole.GetComponent<MeshRenderer>().sharedMaterial = LitMat(new Color(0.4f, 0.3f, 0.2f));
                var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.transform.SetParent(go.transform, false);
                flag.transform.localPosition = new Vector3(0.3f, 3.2f, 0);
                flag.transform.localScale = new Vector3(0.5f, 0.35f, 0.04f);
                flag.GetComponent<MeshRenderer>().sharedMaterial = LitMat(team == Team.Player ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f));
            }

            foreach (var col in go.GetComponentsInChildren<Collider>()) Object.Destroy(col);

            var aim = new GameObject("Aim");
            aim.transform.SetParent(go.transform, false);
            aim.transform.localPosition = new Vector3(0, king ? 1.6f : 1.2f, 0);

            var tower = go.AddComponent<Tower>();
            tower.aimPoint = aim.transform;
            tower.InitTower(team, king);
            return tower;
        }

        static void ConfigureCamera()
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            // Clash Royale-ish 3/4 angle from behind the player's tower. Pulled
            // way back so both halves of the arena are clearly visible at once.
            cam.transform.position = new Vector3(0f, 17.5f, -14.5f);
            cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.45f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
        }

        static void ConfigureLight()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.96f, 0.85f);
            lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
        }
    }
}
