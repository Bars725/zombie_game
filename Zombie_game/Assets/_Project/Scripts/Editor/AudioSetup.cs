using System.Linq;
using Game.Gameplay.Audio;
using Game.Gameplay.Combat;
using Game.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Tools > Audio > Apply Imported Audio: groups clips in Audio/SFX by name (SFX_Pistol_Shot_00001, _00002 ...)
    /// into SoundCue assets (variants = random pick) and assigns them to weapons, enemies and the player prefab.
    /// Safe to run again after adding new files.
    /// </summary>
    public static class AudioSetup
    {
        private const string SfxFolder = "Assets/_Project/Audio/SFX";
        private const string CueFolder = "Assets/_Project/Data/Audio";

        [MenuItem("Tools/Audio/Apply Imported Audio")]
        public static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(CueFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Audio");

            SoundCue pistol = Cue("SFX_Pistol_Shot", 0.7f, 0f);
            SoundCue smg = Cue("SFX_SMG_Shot", 0.55f, 0f);
            SoundCue shotgun = Cue("SFX_Shotgun_Shot", 0.8f, 0f);
            SoundCue reload = Cue("SFX_Reload", 0.6f, 0f);
            SoundCue dry = Cue("SFX_DryFire", 0.6f, 0f);
            SoundCue hitFlesh = Cue("SFX_Hit_Flesh", 0.6f, 1f);
            SoundCue hitMetal = Cue("SFX_Hit_Metal", 0.45f, 1f);
            SoundCue growl = Cue("SFX_Husk_Growl", 0.8f, 1f);
            SoundCue screech = Cue("SFX_Runner_Screech", 0.8f, 1f);
            SoundCue attack = Cue("SFX_Enemy_Attack", 0.7f, 1f);
            SoundCue death = Cue("SFX_Enemy_Death", 0.75f, 1f);
            SoundCue hurt = Cue("SFX_Player_Hurt", 0.7f, 0f);
            AssetDatabase.SaveAssets();

            SetFields("Assets/_Project/Data/Weapons/WD_Pistol.asset", ("_fireSound", pistol), ("_dryFireSound", dry), ("_reloadSound", reload));
            SetFields("Assets/_Project/Data/Weapons/WD_SMG.asset", ("_fireSound", smg), ("_dryFireSound", dry), ("_reloadSound", reload));
            SetFields("Assets/_Project/Data/Weapons/WD_Shotgun.asset", ("_fireSound", shotgun), ("_dryFireSound", dry), ("_reloadSound", reload));
            SetFields("Assets/_Project/Data/Enemies/ED_Husk.asset", ("_alertSound", growl), ("_attackSound", attack), ("_deathSound", death));
            SetFields("Assets/_Project/Data/Enemies/ED_Runner.asset", ("_alertSound", screech), ("_attackSound", attack), ("_deathSound", death));

            const string playerPath = "Assets/_Project/Prefabs/Player/Player.prefab";
            GameObject player = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                if (!player.TryGetComponent(out PlayerAudio audio)) audio = player.AddComponent<PlayerAudio>();
                var so = new SerializedObject(audio);
                so.FindProperty("_weapons").objectReferenceValue = player.GetComponent<PlayerWeaponController>();
                so.FindProperty("_health").objectReferenceValue = player.GetComponent<Health>();
                so.FindProperty("_hitFlesh").objectReferenceValue = hitFlesh;
                so.FindProperty("_hitSurface").objectReferenceValue = hitMetal;
                so.FindProperty("_hurt").objectReferenceValue = hurt;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AudioSetup] Sound cues created and assigned.");
        }

        private static SoundCue Cue(string prefix, float volume, float spatial)
        {
            AudioClip[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p).StartsWith(prefix + "_"))
                .OrderBy(p => p)
                .Select(p => { ConfigureClip(p); return AssetDatabase.LoadAssetAtPath<AudioClip>(p); })
                .Where(c => c != null)
                .ToArray();
            if (clips.Length == 0)
            {
                Debug.LogWarning($"[AudioSetup] No clips for {prefix}");
                return null;
            }

            string path = $"{CueFolder}/SC_{prefix.Substring(4)}.asset";
            var cue = AssetDatabase.LoadAssetAtPath<SoundCue>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<SoundCue>();
                cue.Initialize(clips, volume, spatial);
                AssetDatabase.CreateAsset(cue, path);
            }
            else
            {
                cue.Initialize(clips, cue.Volume, cue.SpatialBlend, cue.MaxDistance); // keep tuned volume/space
                EditorUtility.SetDirty(cue);
            }
            return AssetDatabase.LoadAssetAtPath<SoundCue>(path);
        }

        private static void ConfigureClip(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) return;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            if (importer.forceToMono && s.loadType == AudioClipLoadType.DecompressOnLoad) return;
            importer.forceToMono = true; // 3D positioning needs mono; SFX don't need stereo
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        private static void SetFields(string assetPath, params (string field, Object value)[] fields)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null) return;
            var so = new SerializedObject(asset);
            foreach ((string field, Object value) in fields)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p != null && value != null) p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
