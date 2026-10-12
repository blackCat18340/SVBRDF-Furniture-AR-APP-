#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ARFurniture.EditorTools
{
    // 一鍵：加 Neutral tonemapping(色調映射)，讓亮反射/高光像 Blender 一樣柔和滾降、不爆白。
    // 建一個 VolumeProfile(含 Tonemapping=Neutral) + 場景全域 Volume + 開主相機 Post Processing。
    // 不碰任何模型。注意：AR 下 post-processing 也會套到相機實景背景。
    public static class AddNeutralTonemapping
    {
        const string ProfilePath = "Assets/Lighting/NeutralTonemapProfile.asset";

        [MenuItem("Tools/Add Neutral Tonemapping")]
        public static void Apply()
        {
            // 1) VolumeProfile + Tonemapping=Neutral
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Lighting")) AssetDatabase.CreateFolder("Assets", "Lighting");
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            if (!profile.TryGet<Tonemapping>(out var tm)) tm = profile.Add<Tonemapping>(true);
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.Neutral;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // 2) 場景全域 Volume
            Volume vol = null;
            foreach (var v in Object.FindObjectsOfType<Volume>())
                if (v.isGlobal && v.sharedProfile == profile) { vol = v; break; }
            if (vol == null)
            {
                var go = new GameObject("Global Volume (Tonemapping)");
                vol = go.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 1f;
            }
            vol.sharedProfile = profile;

            // 3) 開主相機 Post Processing
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Tonemapping] 完成：Neutral tonemapping 已加，主相機 Post Processing 已開，已存場景。" +
                      (Camera.main == null ? " (警告：找不到 Camera.main，請手動在 AR 相機勾 Post Processing)" : ""));
        }
    }
}
#endif
