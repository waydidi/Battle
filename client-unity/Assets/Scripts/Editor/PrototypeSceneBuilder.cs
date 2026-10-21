using System.IO;
using System.Linq;
using KingdomsEightBanners.Character;
using KingdomsEightBanners.Combat;
using KingdomsEightBanners.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace KingdomsEightBanners.EditorTools
{
    /// <summary>
    /// Menu: Kingdoms > Build Prototype Desert Town Scene.
    /// Generates the Phase 1 greybox test scene (a small Sunspire market square, a city wall
    /// with a south gate, a slice of Amber Desert, a ramp, steps, pillars and training dummies)
    /// plus the player, camera and HUD, all wired together. Re-running it rebuilds the scene.
    /// Everything is placeholder geometry made from Unity primitives (no third-party assets).
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype_DesertTown.unity";
        private const string MaterialFolder = "Assets/Art/Environment/PrototypeMaterials";

        [MenuItem("Kingdoms/Build Prototype Desert Town Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var sand = GetMaterial("Sand", new Color(0.86f, 0.74f, 0.52f));
            var sandstone = GetMaterial("Sandstone", new Color(0.80f, 0.62f, 0.42f));
            var darkStone = GetMaterial("DarkStone", new Color(0.55f, 0.43f, 0.32f));
            var cloth = GetMaterial("StallCloth", new Color(0.70f, 0.20f, 0.18f));
            var clothBlue = GetMaterial("StallClothBlue", new Color(0.20f, 0.35f, 0.65f));
            var wood = GetMaterial("Wood", new Color(0.45f, 0.30f, 0.18f));
            var player = GetMaterial("PlayerArmor", new Color(0.25f, 0.40f, 0.75f));
            var dummy = GetMaterial("Dummy", new Color(0.65f, 0.55f, 0.35f));
            var rock = GetMaterial("Rock", new Color(0.50f, 0.42f, 0.36f));

            var environment = new GameObject("Environment").transform;
            BuildGround(environment, sand);
            BuildMarketSquare(environment, sandstone, darkStone, cloth, clothBlue, wood);
            BuildCityWall(environment, sandstone, darkStone);
            BuildMovementTests(environment, darkStone, wood);
            BuildDesertField(environment, sand, rock);
            BuildDummies(dummy);

            var playerGo = BuildPlayer(player);
            SetupCamera(playerGo);
            SetupLighting();

            var hud = new GameObject("HUD").AddComponent<PlayerHud>();
            hud.SetPlayer(playerGo.GetComponent<PlayerCharacter>());
            var skills = new GameObject("Skill Window").AddComponent<SkillWindow>();
            skills.SetPlayer(playerGo.GetComponent<PlayerCharacter>());

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("Built " + ScenePath + ". Press Play to test.");
        }

        // ------------------------------------------------------------------ world

        private static void BuildGround(Transform parent, Material sand)
        {
            // A 240 x 240 m slab. Its top surface is at y = 0.
            Block(parent, "Ground", new Vector3(0f, -0.5f, 0f), new Vector3(240f, 1f, 240f), sand);
        }

        private static void BuildMarketSquare(Transform parent, Material sandstone, Material darkStone,
            Material cloth, Material clothBlue, Material wood)
        {
            var square = new GameObject("Sunspire Market Square").transform;
            square.SetParent(parent, false);

            // Plaza floor, fountain and a ring of sandstone houses.
            Block(square, "Plaza", new Vector3(0f, 0.05f, 0f), new Vector3(36f, 0.1f, 36f), darkStone);
            Block(square, "Fountain Base", new Vector3(0f, 0.4f, 0f), new Vector3(5f, 0.8f, 5f), sandstone);
            Block(square, "Fountain Pillar", new Vector3(0f, 1.6f, 0f), new Vector3(1f, 2.4f, 1f), sandstone);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f + 22.5f;
                if (angle > 135f && angle < 225f) continue; // keep the south side open towards the gate
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                float height = 5f + (i % 3) * 2f;
                var house = Block(square, "House " + i, dir * 26f + Vector3.up * (height / 2f),
                    new Vector3(9f, height, 9f), sandstone);
                house.transform.rotation = Quaternion.LookRotation(-dir);
                Block(house.transform, "Roof", new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.1f, 1.1f), darkStone, local: true);
            }

            // Five market stalls (the blueprint's rentable stalls, still just props here).
            for (int i = 0; i < 5; i++)
            {
                var pos = new Vector3(-12f + i * 6f, 0f, 12f);
                var stall = new GameObject("Market Stall " + (i + 1)).transform;
                stall.SetParent(square, false);
                stall.position = pos;
                Block(stall, "Counter", pos + new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 1.2f), wood);
                Block(stall, "Post L", pos + new Vector3(-1.4f, 1.25f, -0.5f), new Vector3(0.15f, 2.5f, 0.15f), wood);
                Block(stall, "Post R", pos + new Vector3(1.4f, 1.25f, -0.5f), new Vector3(0.15f, 2.5f, 0.15f), wood);
                Block(stall, "Awning", pos + new Vector3(0f, 2.55f, -0.2f), new Vector3(3.4f, 0.1f, 2f),
                    i % 2 == 0 ? cloth : clothBlue);
            }
        }

        private static void BuildCityWall(Transform parent, Material sandstone, Material darkStone)
        {
            var wall = new GameObject("City Wall").transform;
            wall.SetParent(parent, false);
            const float half = 40f, height = 7f, thickness = 2f, gateWidth = 8f;

            Block(wall, "North Wall", new Vector3(0f, height / 2f, half), new Vector3(half * 2f, height, thickness), sandstone);
            Block(wall, "East Wall", new Vector3(half, height / 2f, 0f), new Vector3(thickness, height, half * 2f), sandstone);
            Block(wall, "West Wall", new Vector3(-half, height / 2f, 0f), new Vector3(thickness, height, half * 2f), sandstone);

            float segment = half - gateWidth / 2f;
            Block(wall, "South Wall West", new Vector3(-(gateWidth / 2f + segment / 2f), height / 2f, -half),
                new Vector3(segment, height, thickness), sandstone);
            Block(wall, "South Wall East", new Vector3(gateWidth / 2f + segment / 2f, height / 2f, -half),
                new Vector3(segment, height, thickness), sandstone);
            Block(wall, "South Gate Arch", new Vector3(0f, height - 1f, -half), new Vector3(gateWidth, 2f, thickness + 0.5f), darkStone);
            Block(wall, "Gate Tower W", new Vector3(-gateWidth / 2f - 1.5f, height / 2f + 1.5f, -half), new Vector3(3f, height + 3f, 3f), darkStone);
            Block(wall, "Gate Tower E", new Vector3(gateWidth / 2f + 1.5f, height / 2f + 1.5f, -half), new Vector3(3f, height + 3f, 3f), darkStone);
        }

        private static void BuildMovementTests(Transform parent, Material darkStone, Material wood)
        {
            var tests = new GameObject("Movement Test Props").transform;
            tests.SetParent(parent, false);

            // 30 degree ramp (walkable) and 60 degree ramp (too steep: CharacterController slope limit is 45).
            var ramp = Block(tests, "Ramp 30deg", new Vector3(-28f, 2.9f, -20f), new Vector3(4f, 0.4f, 12f), wood);
            ramp.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);
            var steep = Block(tests, "Ramp 60deg (too steep)", new Vector3(-20f, 3.0f, -20f), new Vector3(4f, 0.4f, 7f), darkStone);
            steep.transform.rotation = Quaternion.Euler(-60f, 0f, 0f);

            // Steps of 0.3 m (climbable) leading onto a platform.
            for (int i = 0; i < 6; i++)
            {
                float stepHeight = 0.3f * (i + 1);
                Block(tests, "Step " + i, new Vector3(28f, stepHeight / 2f, -26f + i * 1f),
                    new Vector3(4f, stepHeight, 1f), darkStone);
            }

            // Pillars to check the camera does not clip through geometry.
            for (int i = 0; i < 4; i++)
            {
                Block(tests, "Pillar " + i, new Vector3(-8f + i * 5.5f, 3f, -28f), new Vector3(1.2f, 6f, 1.2f), darkStone);
            }
        }

        private static void BuildDesertField(Transform parent, Material sand, Material rock)
        {
            var field = new GameObject("Amber Desert Field").transform;
            field.SetParent(parent, false);

            var random = new System.Random(8);
            for (int i = 0; i < 14; i++)
            {
                var pos = new Vector3(random.Next(-100, 100), 0f, random.Next(-115, -55));
                float size = 8f + (float)random.NextDouble() * 18f;
                var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dune.name = "Dune " + i;
                dune.transform.SetParent(field, false);
                dune.transform.position = pos + Vector3.down * size * 0.38f;
                dune.transform.localScale = new Vector3(size, size * 0.9f, size * 1.3f);
                dune.GetComponent<Renderer>().sharedMaterial = sand;
                // Sphere colliders ignore non-uniform scale; a mesh collider matches the squashed shape.
                Object.DestroyImmediate(dune.GetComponent<Collider>());
                dune.AddComponent<MeshCollider>().sharedMesh = dune.GetComponent<MeshFilter>().sharedMesh;
                dune.isStatic = true;
            }

            for (int i = 0; i < 20; i++)
            {
                var pos = new Vector3(random.Next(-90, 90), 0.5f, random.Next(-110, -50));
                var rockGo = Block(field, "Rock " + i, pos, Vector3.one * (1f + (float)random.NextDouble() * 2f), rock);
                rockGo.transform.rotation = Quaternion.Euler(random.Next(0, 40), random.Next(0, 360), random.Next(0, 40));
            }

            // Ruined outpost (future quest location) and the future claimable plot marker.
            Block(field, "Ruined Outpost Wall A", new Vector3(30f, 1.5f, -80f), new Vector3(10f, 3f, 1f), rock);
            Block(field, "Ruined Outpost Wall B", new Vector3(25.5f, 1f, -84f), new Vector3(1f, 2f, 8f), rock);
            var plot = Block(field, "Settlement Plot A (marker)", new Vector3(-40f, 0.02f, -85f), new Vector3(20f, 0.04f, 20f), rock);
            Object.DestroyImmediate(plot.GetComponent<Collider>());
        }

        private static void BuildDummies(Material material)
        {
            var parent = new GameObject("Training Dummies").transform;
            var positions = new[] { new Vector3(-6f, 0f, -14f), new Vector3(0f, 0f, -16f), new Vector3(6f, 0f, -14f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummy.name = "Training Dummy " + (i + 1);
                dummy.transform.SetParent(parent, false);
                dummy.transform.position = positions[i] + Vector3.up;
                dummy.GetComponent<Renderer>().sharedMaterial = material;
                dummy.AddComponent<Damageable>();
            }
        }

        // ------------------------------------------------------------------ player & camera

        private static GameObject BuildPlayer(Material armor)
        {
            var player = new GameObject("Player (Storm)");
            player.transform.position = new Vector3(0f, 0.05f, -8f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.05f;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<PlayerCharacter>();
            player.AddComponent<ThirdPersonController>();
            player.AddComponent<PlayerCombat>();

            // Placeholder body: replace with a licensed rigged humanoid later (docs/CONTENT_PIPELINE.md).
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Placeholder Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = armor;

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Facing Marker";
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.transform.SetParent(player.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.55f, 0.3f);
            visor.transform.localScale = new Vector3(0.4f, 0.12f, 0.15f);

            var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Placeholder Sword";
            Object.DestroyImmediate(sword.GetComponent<Collider>());
            sword.transform.SetParent(player.transform, false);
            sword.transform.localPosition = new Vector3(0.45f, 1.0f, 0.35f);
            sword.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            sword.transform.localScale = new Vector3(0.08f, 0.08f, 1.1f);

            AttachStormModel(player, body, visor, sword);
            return player;
        }

        /// <summary>
        /// If the Blender-made Storm model has been exported into the project, use it instead of the
        /// capsule placeholder and give it an Animator Controller (idle/walk/run/sit/sleep/attack/dodge).
        /// </summary>
        private static void AttachStormModel(GameObject player, params GameObject[] placeholders)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(StormModelPostprocessor.ModelPath);
            if (model == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Storm Model";
            instance.transform.SetParent(player.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = StormCharacterSetup.CreateOrUpdateController();

            foreach (var placeholder in placeholders) Object.DestroyImmediate(placeholder);
        }

        private static void SetupCamera(GameObject player)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 600f;
            camera.fieldOfView = 60f;
            camera.transform.position = player.transform.position + new Vector3(0f, 3f, 5f);
            var orbit = camera.gameObject.AddComponent<ThirdPersonCamera>();
            orbit.SetTarget(player.transform, player.GetComponent<PlayerInputReader>());
        }

        private static void SetupLighting()
        {
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                sun.color = new Color(1f, 0.93f, 0.80f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.93f, 0.84f, 0.68f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local)
            {
                go.transform.localPosition = position;
                go.transform.localScale = scale;
            }
            else
            {
                go.transform.position = position;
                go.transform.localScale = scale;
            }

            go.GetComponent<Renderer>().sharedMaterial = material;
            go.isStatic = true;
            return go;
        }

        /// <summary>Creates (or reuses) a coloured material asset that works in URP or the built-in pipeline.</summary>
        private static Material GetMaterial(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                Directory.CreateDirectory(MaterialFolder);
                AssetDatabase.Refresh();
            }

            string path = MaterialFolder + "/" + name + ".mat";
            var shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
