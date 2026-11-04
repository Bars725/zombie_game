using System;
using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.Enemies;
using Game.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.EditorTools
{
    /// <summary>
    /// Wires AI-generated art (ComfyUI / Tripo) into the game: import settings, URP materials,
    /// enemy visuals, weapon view models. Safe to run repeatedly.
    /// Menu: Tools > Art > Apply Imported Art.
    ///
    /// Orientation and scale are detected from the mesh, because generated models come in arbitrary
    /// axes and are normalized to a 1 m cube:
    ///   characters: arm span goes sideways (X), toes point forward (+Z), feet on the ground, given height
    ///   weapons:    longest axis = barrel, muzzle = the thinner end, grip points down, given length
    /// </summary>
    public static class ArtSetup
    {
        private const string ArtRoot = "Assets/_Project/Art";
        private const string HuskFolder = ArtRoot + "/Characters/Husk";
        private const string PistolFolder = ArtRoot + "/Weapons/Pistol";
        private const string HuskPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Husk.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string ViewModelFolder = "Assets/_Project/Prefabs/Weapons";
        private const string PistolDefinitionPath = "Assets/_Project/Data/Weapons/WD_Pistol.asset";

        private const float HuskHeight = 1.8f;
        private const float PistolLength = 0.21f;

        [MenuItem("Tools/Art/Apply Imported Art")]
        public static void ApplyAll()
        {
            bool any = false;
            any |= ApplyHusk();
            any |= ApplyPistol();
            EnsurePlayerViewModelRig();
            AssetDatabase.SaveAssets();
            Debug.Log(any ? "[ArtSetup] Done." : "[ArtSetup] No art found under " + ArtRoot);
        }

        // ------------------------------------------------------------ Husk

        private static bool ApplyHusk()
        {
            string modelPath = HuskFolder + "/SK_Husk.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return false;

            ConfigureModel(modelPath);
            model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); // reimport invalidates the old reference
            Material material = CreateLitMaterial(HuskFolder + "/M_Husk.mat",
                HuskFolder + "/T_Husk_BaseColor.png", HuskFolder + "/T_Husk_Normal.png", null, smoothness: 0.2f);

            GameObject prefab = PrefabUtility.LoadPrefabContents(HuskPrefabPath);
            try
            {
                var controller = prefab.GetComponent<EnemyController>();
                Transform visual = controller != null ? controller.Visual : prefab.transform.Find("Visual");
                if (visual == null) throw new InvalidOperationException("Enemy_Husk has no Visual child");

                for (int i = visual.childCount - 1; i >= 0; i--) Object.DestroyImmediate(visual.GetChild(i).gameObject);

                Transform wrapper = new GameObject("Model").transform;
                wrapper.SetParent(visual, false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, wrapper);
                instance.name = "SK_Husk";
                AssignMaterial(instance, material);
                FitCharacter(wrapper, HuskHeight);

                // Head hitbox to the model's head (head centre ~93% of height on this mesh).
                Transform head = prefab.transform.Find("HeadHitbox");
                if (head != null && head.TryGetComponent(out SphereCollider sphere))
                {
                    sphere.center = new Vector3(0f, HuskHeight * 0.92f, 0f);
                    sphere.radius = 0.14f;
                }
                if (prefab.TryGetComponent(out CapsuleCollider body))
                {
                    body.center = new Vector3(0f, HuskHeight * 0.45f, 0f);
                    body.height = HuskHeight * 0.9f;
                    body.radius = 0.3f;
                }
                if (!prefab.TryGetComponent(out EnemyProceduralAnimator _)) prefab.AddComponent<EnemyProceduralAnimator>();

                PrefabUtility.SaveAsPrefabAsset(prefab, HuskPrefabPath);
                Debug.Log("[ArtSetup] Enemy_Husk now uses SK_Husk");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            // Runner keeps its primitives for now but gets the same procedural motion.
            AddAnimatorTo("Assets/_Project/Prefabs/Enemies/Enemy_Runner.prefab");
            return true;
        }

        private static void AddAnimatorTo(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return;
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!prefab.TryGetComponent(out EnemyProceduralAnimator _))
                {
                    prefab.AddComponent<EnemyProceduralAnimator>();
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        // ------------------------------------------------------------ Pistol

        private static bool ApplyPistol()
        {
            string modelPath = PistolFolder + "/SM_Pistol.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return false;

            ConfigureModel(modelPath);
            model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath); // reimport invalidates the old reference
            Material material = CreateLitMaterial(PistolFolder + "/M_Pistol.mat",
                PistolFolder + "/T_Pistol_BaseColor.png", PistolFolder + "/T_Pistol_Normal.png",
                PistolFolder + "/T_Pistol_MetallicSmoothness.png", smoothness: 1f);

            EnsureFolder(ViewModelFolder);
            string vmPath = ViewModelFolder + "/VM_Pistol.prefab";

            var root = new GameObject("VM_Pistol");
            try
            {
                Transform wrapper = new GameObject("Model").transform;
                wrapper.SetParent(root.transform, false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, wrapper);
                instance.name = "SM_Pistol";
                AssignMaterial(instance, material);
                FitWeapon(wrapper, PistolLength);
                PrefabUtility.SaveAsPrefabAsset(root, vmPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolDefinitionPath);
            if (definition != null)
            {
                var so = new SerializedObject(definition);
                so.FindProperty("_viewModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(vmPath);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Debug.Log("[ArtSetup] VM_Pistol created and assigned to WD_Pistol");
            return true;
        }

        /// <summary>Camera > ViewmodelSocket > (placeholder, view models) + WeaponViewModel component.</summary>
        private static void EnsurePlayerViewModelRig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null) return;
            GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var camera = prefab.GetComponentInChildren<Camera>(true);
                var weapons = prefab.GetComponent<PlayerWeaponController>();
                if (camera == null || weapons == null) return;

                Transform socket = camera.transform.Find("ViewmodelSocket");
                Transform placeholder = camera.transform.Find("ViewmodelPlaceholder");
                if (socket == null)
                {
                    socket = new GameObject("ViewmodelSocket").transform;
                    socket.SetParent(camera.transform, false);
                    socket.localPosition = new Vector3(0.2f, -0.17f, 0.42f);
                    if (placeholder != null)
                    {
                        placeholder.SetParent(socket, false);
                        placeholder.localPosition = Vector3.zero;
                    }
                }
                if (placeholder == null) placeholder = socket.Find("ViewmodelPlaceholder");

                if (!camera.TryGetComponent(out WeaponViewModel vm)) vm = camera.gameObject.AddComponent<WeaponViewModel>();
                var so = new SerializedObject(vm);
                so.FindProperty("_weapons").objectReferenceValue = weapons;
                so.FindProperty("_socket").objectReferenceValue = socket;
                so.FindProperty("_placeholder").objectReferenceValue = placeholder != null ? placeholder.gameObject : null;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        // ------------------------------------------------------------ fitting

        private static void FitCharacter(Transform wrapper, float height)
        {
            wrapper.localPosition = Vector3.zero;
            wrapper.localRotation = Quaternion.identity;
            wrapper.localScale = Vector3.one;

            List<Vector3> pts = CollectPoints(wrapper);
            Bounds b = BoundsOf(pts);

            // Arm span (T-pose) must run along X.
            Quaternion rot = b.size.z > b.size.x ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            pts = pts.Select(p => rot * p).ToList();
            b = BoundsOf(pts);

            // Toes point forward: the lowest slice of the mesh sits in front of the body centre.
            float sliceTop = b.min.y + b.size.y * 0.05f;
            Vector3[] feet = pts.Where(p => p.y <= sliceTop).ToArray();
            if (feet.Length > 0 && feet.Average(p => p.z) < b.center.z)
            {
                rot = Quaternion.Euler(0f, 180f, 0f) * rot;
                pts = pts.Select(p => Quaternion.Euler(0f, 180f, 0f) * p).ToList();
                b = BoundsOf(pts);
            }

            float scale = height / Mathf.Max(0.0001f, b.size.y);
            wrapper.localRotation = rot;
            wrapper.localScale = Vector3.one * scale;
            wrapper.localPosition = -scale * new Vector3(b.center.x, b.min.y, b.center.z);
        }

        private static void FitWeapon(Transform wrapper, float length)
        {
            wrapper.localPosition = Vector3.zero;
            wrapper.localRotation = Quaternion.identity;
            wrapper.localScale = Vector3.one;

            List<Vector3> pts = CollectPoints(wrapper);
            Bounds b = BoundsOf(pts);

            int barrel = LargestAxis(b.size, -1);
            int grip = LargestAxis(b.size, barrel);
            float mid = b.center[barrel];

            // The half with the smaller extent along the grip axis is the muzzle end.
            Vector3[] plus = pts.Where(p => p[barrel] >= mid).ToArray();
            Vector3[] minus = pts.Where(p => p[barrel] < mid).ToArray();
            float extentPlus = plus.Length > 0 ? plus.Max(p => p[grip]) - plus.Min(p => p[grip]) : 0f;
            float extentMinus = minus.Length > 0 ? minus.Max(p => p[grip]) - minus.Min(p => p[grip]) : 0f;
            bool muzzleIsPlus = extentPlus < extentMinus;
            Vector3[] front = muzzleIsPlus ? plus : minus;
            Vector3[] rear = muzzleIsPlus ? minus : plus;

            Vector3 forward = Vector3.zero;
            forward[barrel] = muzzleIsPlus ? 1f : -1f;

            // The grip hangs away from the slide: slide = front-half centre, grip = rear extreme.
            float slide = front.Length > 0 ? front.Average(p => p[grip]) : b.center[grip];
            float rearMin = rear.Length > 0 ? rear.Min(p => p[grip]) : b.min[grip];
            float rearMax = rear.Length > 0 ? rear.Max(p => p[grip]) : b.max[grip];
            bool gripIsMinus = Mathf.Abs(rearMin - slide) > Mathf.Abs(rearMax - slide);
            Vector3 up = Vector3.zero;
            up[grip] = gripIsMinus ? 1f : -1f;

            Quaternion rot = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            pts = pts.Select(p => rot * p).ToList();
            b = BoundsOf(pts);

            float scale = length / Mathf.Max(0.0001f, b.size.z);
            wrapper.localRotation = rot;
            wrapper.localScale = Vector3.one * scale;
            wrapper.localPosition = -scale * b.center;
        }

        private static List<Vector3> CollectPoints(Transform space)
        {
            var points = new List<Vector3>();
            foreach (MeshFilter mf in space.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    points.Add(space.InverseTransformPoint(mf.transform.TransformPoint(v)));
                }
            }
            foreach (SkinnedMeshRenderer smr in space.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                foreach (Vector3 v in smr.sharedMesh.vertices)
                {
                    points.Add(space.InverseTransformPoint(smr.transform.TransformPoint(v)));
                }
            }
            if (points.Count == 0) throw new InvalidOperationException($"No mesh vertices under '{space.name}'");
            return points;
        }

        private static Bounds BoundsOf(List<Vector3> pts)
        {
            var b = new Bounds(pts[0], Vector3.zero);
            foreach (Vector3 p in pts) b.Encapsulate(p);
            return b;
        }

        private static int LargestAxis(Vector3 size, int exclude)
        {
            int best = -1;
            for (int i = 0; i < 3; i++)
            {
                if (i == exclude) continue;
                if (best < 0 || size[i] > size[best]) best = i;
            }
            return best;
        }

        // ------------------------------------------------------------ import / materials

        private static void ConfigureModel(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;
            bool dirty = false;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None; // we assign our own URP material
                dirty = true;
            }
            if (importer.importAnimation) { importer.importAnimation = false; dirty = true; }
            if (importer.importCameras) { importer.importCameras = false; dirty = true; }
            if (importer.importLights) { importer.importLights = false; dirty = true; }
            if (dirty) importer.SaveAndReimport();
        }

        private static Material CreateLitMaterial(string path, string baseMap, string normalMap, string metallicSmoothness,
            float smoothness)
        {
            ConfigureTexture(baseMap, srgb: true, normal: false);
            ConfigureTexture(normalMap, srgb: false, normal: true);
            ConfigureTexture(metallicSmoothness, srgb: false, normal: false);

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", Load<Texture2D>(baseMap));
            material.SetColor("_BaseColor", Color.white);

            Texture2D normal = Load<Texture2D>(normalMap);
            material.SetTexture("_BumpMap", normal);
            SetKeyword(material, "_NORMALMAP", normal != null);

            Texture2D ms = Load<Texture2D>(metallicSmoothness);
            material.SetTexture("_MetallicGlossMap", ms);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", ms != null);
            material.SetFloat("_Metallic", ms != null ? 1f : 0f);
            material.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTexture(string path, bool srgb, bool normal)
        {
            if (string.IsNullOrEmpty(path) || !(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && importer.sRGBTexture == srgb && importer.maxTextureSize == 2048) return;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void AssignMaterial(GameObject root, Material material)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }

        private static T Load<T>(string path) where T : Object =>
            string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
