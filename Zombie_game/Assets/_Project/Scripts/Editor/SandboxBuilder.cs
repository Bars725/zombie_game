using System;
using System.Linq;
using Game.Gameplay.Combat;
using Game.Gameplay.DevTools;
using Game.Gameplay.Enemies;
using Game.Gameplay.Player;
using Game.Gameplay.Weapons;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.EditorTools
{
    /// <summary>
    /// Generates the shooting range scene and everything it needs, so the sandbox is reproducible:
    ///   - layers Player / Enemy / Interactable
    ///   - Data/Player/PMC_Default, Data/Weapons/WD_Pistol, WD_SMG, WD_Shotgun   (created only if missing)
    ///   - Data/Enemies/ED_Husk, ED_Runner + Prefabs/Enemies/*                    (created only if missing)
    ///   - Prefabs/Player/Player.prefab (created if missing; missing components are added)
    ///   - Scenes/Sandbox_TestRange.unity                                        (always regenerated)
    /// Menu: Tools > Sandbox. Batchmode: -executeMethod Game.EditorTools.SandboxBuilder.RebuildFromCommandLine
    /// Existing data assets are never overwritten, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class SandboxBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Sandbox_TestRange.unity";
        private const string PlayerPrefabPath = Root + "/Prefabs/Player/Player.prefab";
        private const string MovementConfigPath = Root + "/Data/Player/PMC_Default.asset";
        private const string WeaponsFolder = Root + "/Data/Weapons";
        private const string MaterialsFolder = Root + "/Materials/Sandbox";
        private const string EnemiesDataFolder = Root + "/Data/Enemies";
        private const string EnemiesPrefabFolder = Root + "/Prefabs/Enemies";
        private const int IgnoreRaycastLayer = 2;

        private static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, -5f);

        [MenuItem("Tools/Sandbox/Rebuild Test Range")]
        public static void RebuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(overwritePlayerPrefab: false);
        }

        [MenuItem("Tools/Sandbox/Rebuild Test Range + Recreate Player Prefab")]
        public static void RebuildWithPlayerFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(overwritePlayerPrefab: true);
        }

        /// <summary>Batchmode entry. Add -overwritePlayerPrefab to recreate the player prefab.</summary>
        public static void RebuildFromCommandLine()
        {
            try
            {
                bool overwrite = Environment.GetCommandLineArgs()
                    .Any(a => string.Equals(a, "-overwritePlayerPrefab", StringComparison.OrdinalIgnoreCase));
                Build(overwrite);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Build(bool overwritePlayerPrefab)
        {
            EnsureLayer(8, "Player");
            EnsureLayer(9, "Enemy");
            EnsureLayer(10, "Interactable");

            EnsureFolder(Root + "/Data/Player");
            EnsureFolder(WeaponsFolder);
            EnsureFolder(Root + "/Prefabs/Player");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MaterialsFolder);
            EnsureFolder(EnemiesDataFolder);
            EnsureFolder(EnemiesPrefabFolder);

            PlayerMovementConfig config = LoadOrCreate<PlayerMovementConfig>(MovementConfigPath, null);
            WeaponDefinition[] loadout =
            {
                LoadOrCreate<WeaponDefinition>($"{WeaponsFolder}/WD_Pistol.asset", w => w.Initialize(
                    "pistol", "Pistol", damage: 25f, FireMode.SemiAuto, roundsPerMinute: 400f,
                    magazineSize: 12, maxReserve: 96, startingReserve: 48, reloadTime: 1.4f,
                    spreadDegrees: 0.6f, range: 100f, falloffStart: 15f, falloffEnd: 40f, minDamageMultiplier: 0.6f,
                    price: 0)),
                LoadOrCreate<WeaponDefinition>($"{WeaponsFolder}/WD_SMG.asset", w => w.Initialize(
                    "smg", "SMG", damage: 14f, FireMode.FullAuto, roundsPerMinute: 750f,
                    magazineSize: 30, maxReserve: 240, startingReserve: 120, reloadTime: 1.8f,
                    spreadDegrees: 2f, range: 80f, falloffStart: 10f, falloffEnd: 30f, minDamageMultiplier: 0.5f,
                    price: 1000)),
                LoadOrCreate<WeaponDefinition>($"{WeaponsFolder}/WD_Shotgun.asset", w => w.Initialize(
                    "shotgun", "Shotgun", damage: 12f, FireMode.SemiAuto, roundsPerMinute: 70f,
                    magazineSize: 6, maxReserve: 48, startingReserve: 24, reloadTime: 2.6f,
                    pelletsPerShot: 8, spreadDegrees: 6f, range: 40f, falloffStart: 5f, falloffEnd: 15f,
                    minDamageMultiplier: 0.3f, price: 1500)),
            };

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null || overwritePlayerPrefab)
            {
                playerPrefab = CreatePlayerPrefab(config, loadout);
            }
            EnsurePlayerPrefabComponents();

            Materials mats = CreateMaterials();
            AssetDatabase.SaveAssets();
            EnemyDefinition[] enemies =
            {
                LoadOrCreate<EnemyDefinition>($"{EnemiesDataFolder}/ED_Husk.asset", d => d.Initialize(
                    "husk", "Husk", LoadOrCreateEnemyPrefab("Enemy_Husk", mats.Husk, 1f),
                    maxHealth: 100f, moveSpeed: 2.2f, attackDamage: 20f, attackRange: 1.6f,
                    attackWindup: 0.5f, attackRecovery: 0.9f, acceleration: 10f, spawnCost: 1, scrapOnKill: 60)),
                LoadOrCreate<EnemyDefinition>($"{EnemiesDataFolder}/ED_Runner.asset", d => d.Initialize(
                    "runner", "Runner", LoadOrCreateEnemyPrefab("Enemy_Runner", mats.Runner, 0.85f),
                    maxHealth: 60f, moveSpeed: 5f, attackDamage: 12f, attackRange: 1.5f,
                    attackWindup: 0.35f, attackRecovery: 0.6f, acceleration: 20f, spawnCost: 2, scrapOnKill: 90)),
            };

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null) Object.DestroyImmediate(defaultCamera); // the player has its own camera

            BuildEnvironment(mats);
            BuildTargets(mats);
            // NewScene can unload assets created earlier in this call; reload them from disk
            // so the scene does not serialize dead (null) references.
            BuildEnemySpawning(new[] { $"{EnemiesDataFolder}/ED_Husk.asset", $"{EnemiesDataFolder}/ED_Runner.asset" }
                .Select(LoadRequired<EnemyDefinition>).ToArray());

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetPositionAndRotation(PlayerSpawn, Quaternion.identity);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log($"[SandboxBuilder] Built {ScenePath}. Press Play, click the Game view, shoot the targets.");
        }

        // ---------------------------------------------------------------- player

        private static GameObject CreatePlayerPrefab(PlayerMovementConfig config, WeaponDefinition[] loadout)
        {
            int playerLayer = LayerMask.NameToLayer("Player");

            var root = new GameObject("Player");
            root.tag = "Player";
            if (playerLayer >= 0) root.layer = playerLayer;

            var controller = root.AddComponent<CharacterController>();
            controller.height = config.StandHeight;
            controller.center = new Vector3(0f, config.StandHeight * 0.5f, 0f);
            controller.radius = 0.35f;
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 45f;

            Transform pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, config.StandHeight - config.EyeOffsetFromTop, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(pivot, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 75f;
            cameraGo.AddComponent<AudioListener>();

            GameObject viewmodel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            viewmodel.name = "ViewmodelPlaceholder";
            Object.DestroyImmediate(viewmodel.GetComponent<Collider>());
            viewmodel.transform.SetParent(cameraGo.transform, false);
            viewmodel.transform.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
            viewmodel.transform.localScale = new Vector3(0.08f, 0.1f, 0.35f);
            if (playerLayer >= 0) viewmodel.layer = playerLayer;

            var input = root.AddComponent<PlayerInputReader>();
            var look = root.AddComponent<PlayerLook>();
            var motor = root.AddComponent<PlayerMotor>();
            var health = root.AddComponent<Health>();
            var weapons = root.AddComponent<PlayerWeaponController>();
            var hud = root.AddComponent<DebugHud>();

            health.Configure(maxHealth: 100f, regenPerSecond: 12f, regenDelay: 4f, segmentFraction: 0.25f);

            int shootMask = Physics.DefaultRaycastLayers;
            if (playerLayer >= 0) shootMask &= ~(1 << playerLayer);

            SetRefs(look, ("_input", input), ("_cameraPivot", pivot));
            SetRefs(motor, ("_config", config), ("_input", input), ("_cameraPivot", pivot));
            SetRefs(weapons, ("_input", input), ("_camera", camera));
            SetRefs(hud, ("_health", health), ("_motor", motor), ("_weapons", weapons), ("_input", input));
            SetInt(weapons, "_hitMask", shootMask);
            SetArray(weapons, "_startingLoadout", loadout.Cast<Object>().ToArray());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- level

        private struct Materials
        {
            public Material Ground, Wall, Obstacle, Hazard, Target, Husk, Runner;
        }

        private static Materials CreateMaterials() => new Materials
        {
            Ground = LoadOrCreateMaterial("M_Ground", new Color(0.35f, 0.37f, 0.4f)),
            Wall = LoadOrCreateMaterial("M_Wall", new Color(0.55f, 0.57f, 0.6f)),
            Obstacle = LoadOrCreateMaterial("M_Obstacle", new Color(0.3f, 0.45f, 0.6f)),
            Hazard = LoadOrCreateMaterial("M_Hazard", new Color(0.8f, 0.15f, 0.1f)),
            Target = LoadOrCreateMaterial("M_Target", new Color(0.85f, 0.55f, 0.2f)),
            Husk = LoadOrCreateMaterial("M_Husk", new Color(0.35f, 0.5f, 0.3f)),
            Runner = LoadOrCreateMaterial("M_Runner", new Color(0.7f, 0.22f, 0.18f)),
        };

        private static void BuildEnvironment(Materials m)
        {
            Transform env = new GameObject("Environment").transform;

            // NavMesh for enemies, baked at scene start (see RuntimeNavMeshBaker).
            var surface = env.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(LayerBit("Player") | LayerBit("Enemy") | (1 << IgnoreRaycastLayer));
            env.gameObject.AddComponent<RuntimeNavMeshBaker>();

            Box("Ground", env, new Vector3(0f, -0.5f, 10f), new Vector3(60f, 1f, 70f), m.Ground);
            Box("Wall_North", env, new Vector3(0f, 2f, 45f), new Vector3(60f, 4f, 1f), m.Wall);
            Box("Wall_South", env, new Vector3(0f, 2f, -25f), new Vector3(60f, 4f, 1f), m.Wall);
            Box("Wall_East", env, new Vector3(30f, 2f, 10f), new Vector3(1f, 4f, 70f), m.Wall);
            Box("Wall_West", env, new Vector3(-30f, 2f, 10f), new Vector3(1f, 4f, 70f), m.Wall);

            Transform course = new GameObject("MovementCourse").transform;
            Box("Step_0.3m", course, new Vector3(-6f, 0.15f, -2f), new Vector3(2f, 0.3f, 2f), m.Obstacle);
            Box("JumpBox_0.9m", course, new Vector3(-6f, 0.45f, 2f), new Vector3(2f, 0.9f, 2f), m.Obstacle);
            Box("Ramp_15deg", course, new Vector3(7f, 0.9f, 2f), new Vector3(3f, 0.3f, 8f), m.Obstacle, new Vector3(-15f, 0f, 0f));

            // Crouch tunnel: 1.4 m clearance (stand 1.8 m, crouch 1.1 m)
            Box("Tunnel_Ceiling_1.4m", course, new Vector3(13f, 1.55f, 2f), new Vector3(3f, 0.3f, 6f), m.Obstacle);
            Box("Tunnel_SideL", course, new Vector3(11.35f, 0.7f, 2f), new Vector3(0.3f, 1.4f, 6f), m.Obstacle);
            Box("Tunnel_SideR", course, new Vector3(14.65f, 0.7f, 2f), new Vector3(0.3f, 1.4f, 6f), m.Obstacle);

            Box("Cover_A", course, new Vector3(-3f, 0.6f, 7f), new Vector3(2f, 1.2f, 0.6f), m.Obstacle);
            Box("Cover_B", course, new Vector3(3f, 0.6f, 9f), new Vector3(2f, 1.2f, 0.6f), m.Obstacle);

            // Hazard pad: tests player damage and segmented regen
            GameObject pad = Box("DamagePad_10perTick", course, new Vector3(-12f, 0.05f, -2f), new Vector3(3f, 0.1f, 3f), m.Hazard);
            pad.layer = IgnoreRaycastLayer; // not shootable, not a NavMesh obstacle
            var trigger = pad.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 20f, 1f);     // 2 m tall in world space (scale y = 0.1)
            trigger.center = new Vector3(0f, 10f, 0f);
            var body = pad.AddComponent<Rigidbody>();   // kinematic body guarantees trigger events with the CharacterController
            body.isKinematic = true;
            pad.AddComponent<DamageVolume>();
        }

        private static void BuildTargets(Materials m)
        {
            Transform parent = new GameObject("Targets").transform;
            CreateTarget(parent, "Target_8m", new Vector3(-3f, 0f, 3f), m.Target);
            CreateTarget(parent, "Target_18m_L", new Vector3(-5f, 0f, 13f), m.Target);
            CreateTarget(parent, "Target_18m", new Vector3(0f, 0f, 13f), m.Target);
            CreateTarget(parent, "Target_18m_R", new Vector3(5f, 0f, 13f), m.Target);
            CreateTarget(parent, "Target_35m", new Vector3(3f, 0f, 30f), m.Target);
        }

        private static void CreateTarget(Transform parent, string name, Vector3 feetPosition, Material material)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule); // 2 m tall, pivot in the middle
            body.name = name;
            body.transform.SetParent(parent, false);
            body.transform.localPosition = feetPosition + Vector3.up;
            body.GetComponent<Renderer>().sharedMaterial = material;

            var health = body.AddComponent<Health>();
            health.Configure(maxHealth: 100f);
            body.AddComponent<TargetDummy>();

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(body.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale = Vector3.one * 0.45f;
            head.GetComponent<Renderer>().sharedMaterial = material;
            head.AddComponent<HitZone>().Configure(HitZoneType.Head, 2f, health);
        }

        // ---------------------------------------------------------------- enemies

        private static void BuildEnemySpawning(EnemyDefinition[] enemies)
        {
            var root = new GameObject("EnemySpawning");
            var factory = root.AddComponent<EnemyFactory>();
            var spawner = root.AddComponent<DevEnemySpawner>();

            Vector3[] points =
            {
                new Vector3(-22f, 0f, 40f), new Vector3(22f, 0f, 40f),
                new Vector3(0f, 0f, 42f), new Vector3(-25f, 0f, 20f), new Vector3(25f, 0f, 20f),
            };
            var transforms = new Object[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Transform t = new GameObject($"SpawnPoint_{i + 1}").transform;
                t.SetParent(root.transform, false);
                t.position = points[i];
                t.rotation = Quaternion.LookRotation(PlayerSpawn - points[i]);
                transforms[i] = t;
            }

            AssetDatabase.SaveAssets();
            SetRefs(spawner, ("_factory", factory));
            SetArray(spawner, "_definitions", enemies.Cast<Object>().ToArray());
            SetArray(spawner, "_spawnPoints", transforms);
        }

        private static EnemyController LoadOrCreateEnemyPrefab(string name, Material material, float width)
        {
            string path = $"{EnemiesPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<EnemyController>();

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            var root = new GameObject(name);
            if (enemyLayer >= 0) root.layer = enemyLayer;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.baseOffset = 0f;

            var body = root.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.9f, 0f);
            body.height = 1.8f;
            body.radius = 0.35f;

            var health = root.AddComponent<Health>();
            var controller = root.AddComponent<EnemyController>();

            // Visual only (no colliders): moved down/up for emerge and sink.
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            GameObject torso = VisualPrimitive(PrimitiveType.Capsule, "Torso", visual, material);
            torso.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            torso.transform.localScale = new Vector3(0.7f * width, 0.8f, 0.7f * width);
            GameObject headMesh = VisualPrimitive(PrimitiveType.Sphere, "Head", visual, material);
            headMesh.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            headMesh.transform.localScale = Vector3.one * 0.38f;
            GameObject armL = VisualPrimitive(PrimitiveType.Cube, "ArmL", visual, material);
            armL.transform.localPosition = new Vector3(-0.3f * width, 1.15f, 0.35f);
            armL.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f);
            GameObject armR = VisualPrimitive(PrimitiveType.Cube, "ArmR", visual, material);
            armR.transform.localPosition = new Vector3(0.3f * width, 1.15f, 0.35f);
            armR.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f);

            var headHitbox = new GameObject("HeadHitbox");
            headHitbox.transform.SetParent(root.transform, false);
            if (enemyLayer >= 0) headHitbox.layer = enemyLayer;
            var headCollider = headHitbox.AddComponent<SphereCollider>();
            headCollider.center = new Vector3(0f, 1.62f, 0f);
            headCollider.radius = 0.2f;
            headHitbox.AddComponent<HitZone>().Configure(HitZoneType.Head, 2f, health);

            SetRefs(controller, ("_visual", visual));
            SetArray(controller, "_hitColliders", new Object[] { body, headCollider });

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[SandboxBuilder] Created {path}");
            return prefab.GetComponent<EnemyController>();
        }

        private static GameObject VisualPrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Adds components introduced after the prefab was first generated, without touching tuning.</summary>
        private static void EnsurePlayerPrefabComponents()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                bool changed = false;
                if (!contents.TryGetComponent(out PlayerAnchor _)) { contents.AddComponent<PlayerAnchor>(); changed = true; }
                if (!contents.TryGetComponent(out DevPlayerRespawn _)) { contents.AddComponent<DevPlayerRespawn>(); changed = true; }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                    Debug.Log("[SandboxBuilder] Added PlayerAnchor / DevPlayerRespawn to the Player prefab");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static T LoadRequired<T>(string path) where T : Object
        {
            T loaded = AssetDatabase.LoadAssetAtPath<T>(path);
            if (loaded == null)
            {
                throw new InvalidOperationException($"[SandboxBuilder] Could not reload {typeof(T).Name} '{path}'.");
            }
            return loaded;
        }

        private static int LayerBit(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material,
            Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static T LoadOrCreate<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[SandboxBuilder] Created {path}");
            return asset;
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static void EnsureLayer(int index, string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || index >= layers.arraySize) return;

            SerializedProperty slot = layers.GetArrayElementAtIndex(index);
            if (slot.stringValue == layerName) return;
            if (!string.IsNullOrEmpty(slot.stringValue))
            {
                Debug.LogWarning($"[SandboxBuilder] Layer {index} is '{slot.stringValue}', expected '{layerName}'. Left unchanged.");
                return;
            }
            slot.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[SandboxBuilder] Added layer {index}: {layerName}");
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach ((string field, Object value) in refs)
            {
                SerializedProperty p = so.FindProperty(field)
                    ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'");
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'");
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
