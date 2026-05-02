using System.Collections.Generic;
using UnityEngine;
using TrashRoyale.Core;
using TrashRoyale.Util;

namespace TrashRoyale.Match
{
    /// <summary>
    /// Builds chunky 3D character models out of Unity primitives. Each card has
    /// its own silhouette (knight = silver capsule + helmet, pig = pink box with
    /// snout, skibidi = toilet + head etc.) so they're distinguishable from each
    /// other without needing huge glTF imports.
    /// </summary>
    public static class ModelLoader
    {
        public static GameObject InstantiateUnit(CardData card)
        {
            var go = new GameObject(card.id);
            switch (card.id)
            {
                case "knight":   BuildKnight(go); break;
                case "pig":      BuildPig(go); break;
                case "skibidi":  BuildSkibidi(go); break;
                case "pocoyo":   BuildPocoyo(go); break;
                case "amongus":  BuildAmongUs(go); break;
                case "cheems":   BuildCheems(go); break;
                case "shrek":    BuildShrek(go); break;
                case "gigachad": BuildGigachad(go); break;
                case "nyancat":  BuildNyanCat(go); break;
                default:         BuildGeneric(go, card); break;
            }
            return go;
        }

        // ---------- Builders ----------

        static void BuildKnight(GameObject go)
        {
            // Body capsule (armor), head sphere (helmet), tiny sword
            Body(go, new Color(0.85f, 0.85f, 0.9f), 0.85f);
            Head(go, new Color(0.7f, 0.7f, 0.75f), 1.55f, 0.55f);
            // Sword
            var sword = Cube(go, new Color(0.6f, 0.6f, 0.7f), new Vector3(0.12f, 0.7f, 0.12f),
                new Vector3(0.45f, 0.95f, 0.0f));
            // Cape
            Cube(go, new Color(0.85f, 0.2f, 0.25f), new Vector3(0.7f, 0.7f, 0.05f),
                new Vector3(0, 0.95f, -0.35f));
        }

        static void BuildPig(GameObject go)
        {
            // Pink chunky body, snout box on front, two tiny ears
            Cube(go, new Color(1f, 0.65f, 0.7f), new Vector3(1.05f, 0.85f, 1.4f), new Vector3(0, 0.6f, 0));
            // Snout
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.45f, 0.4f, 0.3f), new Vector3(0, 0.7f, 0.78f));
            // Ears
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.18f, 0.25f, 0.1f), new Vector3(-0.3f, 1.1f, 0.4f));
            Cube(go, new Color(1f, 0.55f, 0.6f), new Vector3(0.18f, 0.25f, 0.1f), new Vector3(0.3f, 1.1f, 0.4f));
            // Eyes
            Sphere(go, Color.black, 0.08f, new Vector3(-0.18f, 0.85f, 0.65f));
            Sphere(go, Color.black, 0.08f, new Vector3(0.18f, 0.85f, 0.65f));
        }

        static void BuildSkibidi(GameObject go)
        {
            // White toilet base + bowl, brown head sticking out, tiny eyes
            Cube(go, new Color(0.95f, 0.95f, 0.97f), new Vector3(0.85f, 0.55f, 0.6f), new Vector3(0, 0.3f, 0));
            // Toilet bowl rim
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.transform.SetParent(go.transform, false);
            bowl.transform.localScale = new Vector3(0.85f, 0.07f, 0.65f);
            bowl.transform.localPosition = new Vector3(0, 0.66f, 0);
            Object.DestroyImmediate(bowl.GetComponent<Collider>());
            bowl.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.95f, 0.95f, 0.97f));
            // Skibidi head
            Sphere(go, new Color(0.92f, 0.78f, 0.65f), 0.42f, new Vector3(0, 1.1f, 0.05f));
            // Hair tuft (black)
            Sphere(go, new Color(0.15f, 0.1f, 0.07f), 0.32f, new Vector3(0, 1.45f, -0.05f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.16f, 1.15f, 0.4f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.16f, 1.15f, 0.4f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.16f, 1.15f, 0.5f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.16f, 1.15f, 0.5f));
        }

        static void BuildPocoyo(GameObject go)
        {
            // Blue jumpsuit body, big round head, blue hat (everything blue)
            Cube(go, new Color(0.25f, 0.5f, 0.95f), new Vector3(0.7f, 0.85f, 0.5f), new Vector3(0, 0.55f, 0));
            Sphere(go, new Color(0.95f, 0.75f, 0.6f), 0.55f, new Vector3(0, 1.4f, 0));
            // Hat (cylinder)
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.transform.SetParent(go.transform, false);
            hat.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
            hat.transform.localPosition = new Vector3(0, 1.85f, 0);
            Object.DestroyImmediate(hat.GetComponent<Collider>());
            hat.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.25f, 0.5f, 0.95f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.18f, 1.45f, 0.45f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.18f, 1.45f, 0.45f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.18f, 1.45f, 0.55f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.18f, 1.45f, 0.55f));
        }

        static void BuildAmongUs(GameObject go)
        {
            // Red bean: capsule body, sphere visor on front
            Body(go, new Color(0.95f, 0.2f, 0.2f), 0.95f, 0.55f);
            // Backpack
            Cube(go, new Color(0.7f, 0.15f, 0.15f), new Vector3(0.5f, 0.65f, 0.3f), new Vector3(0, 0.95f, -0.45f));
            // Visor (cyan glossy oval)
            var visor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visor.transform.SetParent(go.transform, false);
            visor.transform.localScale = new Vector3(0.55f, 0.32f, 0.15f);
            visor.transform.localPosition = new Vector3(0, 1.2f, 0.45f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.85f, 1f));
        }

        static void BuildCheems(GameObject go)
        {
            // Tan dog: chubby box body, big round head, ears, snout
            Cube(go, new Color(0.92f, 0.78f, 0.55f), new Vector3(0.85f, 0.7f, 1.1f), new Vector3(0, 0.5f, 0));
            // Head
            Sphere(go, new Color(0.95f, 0.82f, 0.6f), 0.55f, new Vector3(0, 1.1f, 0.4f));
            // Ears
            Cube(go, new Color(0.85f, 0.7f, 0.45f), new Vector3(0.18f, 0.35f, 0.1f), new Vector3(-0.3f, 1.45f, 0.35f));
            Cube(go, new Color(0.85f, 0.7f, 0.45f), new Vector3(0.18f, 0.35f, 0.1f), new Vector3(0.3f, 1.45f, 0.35f));
            // Snout
            Sphere(go, new Color(0.95f, 0.85f, 0.65f), 0.22f, new Vector3(0, 1.0f, 0.85f));
            Sphere(go, Color.black, 0.08f, new Vector3(0, 1.05f, 1.0f));
            // Eyes
            Sphere(go, Color.white, 0.10f, new Vector3(-0.18f, 1.2f, 0.85f));
            Sphere(go, Color.white, 0.10f, new Vector3(0.18f, 1.2f, 0.85f));
            Sphere(go, Color.black, 0.05f, new Vector3(-0.18f, 1.2f, 0.92f));
            Sphere(go, Color.black, 0.05f, new Vector3(0.18f, 1.2f, 0.92f));
        }

        static void BuildShrek(GameObject go)
        {
            // Big green ogre: chunky body, big round head with ear-trumpets
            Cube(go, new Color(0.35f, 0.7f, 0.3f), new Vector3(1.3f, 1.2f, 1.0f), new Vector3(0, 0.7f, 0));
            // Head
            Sphere(go, new Color(0.55f, 0.8f, 0.4f), 0.6f, new Vector3(0, 1.65f, 0));
            // Ear trumpets
            var earL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earL.transform.SetParent(go.transform, false);
            earL.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            earL.transform.localPosition = new Vector3(-0.55f, 1.65f, 0);
            earL.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.DestroyImmediate(earL.GetComponent<Collider>());
            earL.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.8f, 0.4f));
            var earR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earR.transform.SetParent(go.transform, false);
            earR.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            earR.transform.localPosition = new Vector3(0.55f, 1.65f, 0);
            earR.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Object.DestroyImmediate(earR.GetComponent<Collider>());
            earR.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(new Color(0.55f, 0.8f, 0.4f));
            // Eyes
            Sphere(go, Color.white, 0.13f, new Vector3(-0.18f, 1.7f, 0.45f));
            Sphere(go, Color.white, 0.13f, new Vector3(0.18f, 1.7f, 0.45f));
            Sphere(go, Color.black, 0.06f, new Vector3(-0.18f, 1.7f, 0.55f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.18f, 1.7f, 0.55f));
        }

        static void BuildGigachad(GameObject go)
        {
            // Bulky tan torso (huge), small head, big jaw
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(1.6f, 1.1f, 0.7f), new Vector3(0, 0.7f, 0));
            // Arms
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(0.35f, 1.0f, 0.35f), new Vector3(-0.95f, 0.6f, 0));
            Cube(go, new Color(0.85f, 0.65f, 0.5f), new Vector3(0.35f, 1.0f, 0.35f), new Vector3(0.95f, 0.6f, 0));
            // Head
            Sphere(go, new Color(0.92f, 0.78f, 0.62f), 0.4f, new Vector3(0, 1.55f, 0));
            // Hair (black blob)
            Sphere(go, new Color(0.1f, 0.08f, 0.06f), 0.42f, new Vector3(0, 1.78f, -0.05f));
            // Jaw (small cube)
            Cube(go, new Color(0.92f, 0.78f, 0.62f), new Vector3(0.35f, 0.18f, 0.3f), new Vector3(0, 1.3f, 0.2f));
            // Eyes (slits — small black cubes)
            Cube(go, Color.black, new Vector3(0.12f, 0.03f, 0.04f), new Vector3(-0.15f, 1.6f, 0.32f));
            Cube(go, Color.black, new Vector3(0.12f, 0.03f, 0.04f), new Vector3(0.15f, 1.6f, 0.32f));
        }

        static void BuildNyanCat(GameObject go)
        {
            // Pop-tart rectangle body (pink + sprinkles) + cat head, rainbow trail behind
            Cube(go, new Color(1f, 0.7f, 0.85f), new Vector3(1.05f, 0.55f, 0.65f), new Vector3(0, 1.4f, 0));
            // Cat head
            Sphere(go, new Color(0.65f, 0.65f, 0.65f), 0.4f, new Vector3(0, 1.8f, 0.4f));
            // Cat ears (cubes tilted)
            Cube(go, new Color(0.65f, 0.65f, 0.65f), new Vector3(0.18f, 0.2f, 0.05f), new Vector3(-0.18f, 2.05f, 0.4f));
            Cube(go, new Color(0.65f, 0.65f, 0.65f), new Vector3(0.18f, 0.2f, 0.05f), new Vector3(0.18f, 2.05f, 0.4f));
            // Eyes
            Sphere(go, Color.black, 0.06f, new Vector3(-0.13f, 1.85f, 0.7f));
            Sphere(go, Color.black, 0.06f, new Vector3(0.13f, 1.85f, 0.7f));
            // Rainbow trail: stack of colored quads behind
            Color[] rainbow = {
                new Color(1f, 0.2f, 0.2f),
                new Color(1f, 0.6f, 0.2f),
                new Color(1f, 1f, 0.2f),
                new Color(0.2f, 1f, 0.3f),
                new Color(0.3f, 0.6f, 1f),
                new Color(0.6f, 0.3f, 1f),
            };
            for (int i = 0; i < rainbow.Length; i++)
            {
                Cube(go, rainbow[i], new Vector3(0.6f, 0.1f, 0.5f),
                    new Vector3(0, 1.3f + i * 0.1f, -0.55f));
            }
        }

        static void BuildGeneric(GameObject go, CardData card)
        {
            int idx = Mathf.Abs(card.id.GetHashCode()) % 6;
            Color[] colors = {
                new Color(0.95f, 0.85f, 0.4f),
                new Color(1f, 0.5f, 0.5f),
                new Color(0.4f, 0.7f, 1f),
                new Color(0.5f, 0.9f, 0.4f),
                new Color(0.95f, 0.5f, 0.95f),
                new Color(1f, 0.7f, 0.3f),
            };
            Body(go, colors[idx], 0.9f);
            Head(go, colors[idx], 1.55f, 0.55f);
        }

        // ---------- Helpers ----------

        static void Body(GameObject parent, Color c, float h, float w = 0.7f)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = new Vector3(w, h * 0.5f, w);
            p.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
        }

        static void Head(GameObject parent, Color c, float y, float r)
        {
            Sphere(parent, c, r, new Vector3(0, y, 0));
        }

        static GameObject Cube(GameObject parent, Color c, Vector3 size, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = size;
            p.transform.localPosition = pos;
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
            return p;
        }

        static GameObject Sphere(GameObject parent, Color c, float r, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.SetParent(parent.transform, false);
            p.transform.localScale = new Vector3(r * 2, r * 2, r * 2);
            p.transform.localPosition = pos;
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<MeshRenderer>().sharedMaterial = SafeShader.NewOpaqueMaterial(c);
            return p;
        }
    }
}
