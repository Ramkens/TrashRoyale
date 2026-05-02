using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Combat;

namespace TrashRoyale.Match
{
    public static class ArenaBuilder
    {
        public static void Build(MatchManager match, ArenaController arena)
        {
            BuildGround();
            BuildRiver();
            BuildBridges();
            BuildTowers(match);
            ConfigureCamera();
            ConfigureLight();
        }

        static Material LitMat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            return m;
        }

        static void BuildGround()
        {
            var ground = new GameObject("Ground");
            for (int side = 0; side < 2; side++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quad.transform.SetParent(ground.transform, false);
                quad.name = side == 0 ? "PlayerSide" : "EnemySide";
                quad.transform.localScale = new Vector3(ArenaController.HalfWidth * 2f, 0.2f, ArenaController.HalfLength);
                quad.transform.position = new Vector3(0, -0.1f, side == 0 ? -ArenaController.HalfLength * 0.5f : ArenaController.HalfLength * 0.5f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = LitMat(side == 0 ? new Color(0.45f, 0.65f, 0.35f) : new Color(0.55f, 0.4f, 0.6f));
            }
        }

        static void BuildRiver()
        {
            var river = GameObject.CreatePrimitive(PrimitiveType.Cube);
            river.name = "River";
            river.transform.localScale = new Vector3(ArenaController.HalfWidth * 2f, 0.05f, ArenaController.RiverHalfThickness * 2f);
            river.transform.position = new Vector3(0, -0.05f, 0);
            river.GetComponent<MeshRenderer>().sharedMaterial = LitMat(new Color(0.25f, 0.55f, 0.85f));
        }

        static void BuildBridges()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = "Bridge";
                bridge.transform.localScale = new Vector3(1.6f, 0.05f, 1.4f);
                bridge.transform.position = new Vector3(side * 2.8f, -0.02f, 0f);
                bridge.GetComponent<MeshRenderer>().sharedMaterial = LitMat(new Color(0.7f, 0.5f, 0.3f));
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

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, king ? 1.2f : 0.9f, 0);
            visual.transform.localScale = new Vector3(king ? 1.6f : 1.1f, king ? 2.4f : 1.8f, king ? 1.6f : 1.1f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = LitMat(team == Team.Player ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.4f));

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(go.transform, false);
            roof.transform.localPosition = new Vector3(0, king ? 2.5f : 1.9f, 0);
            roof.transform.localScale = new Vector3(king ? 1.8f : 1.3f, 0.2f, king ? 1.8f : 1.3f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = LitMat(team == Team.Player ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.7f, 0.6f));

            foreach (var col in go.GetComponentsInChildren<Collider>()) Object.Destroy(col);

            var aim = new GameObject("Aim");
            aim.transform.SetParent(go.transform, false);
            aim.transform.localPosition = new Vector3(0, king ? 1.4f : 1.0f, 0);

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
            cam.transform.position = new Vector3(0, 11.5f, -7.5f);
            cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            cam.fieldOfView = 50f;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
        }

        static void ConfigureLight()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.85f);
            lightGo.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
        }
    }
}
