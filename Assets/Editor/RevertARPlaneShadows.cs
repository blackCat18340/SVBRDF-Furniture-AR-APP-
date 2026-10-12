#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARFurniture.EditorTools
{
    // 一鍵還原：把 SwitchARPlaneMaterial 的 materialA 設回 DevDunk ShadowReceiver，
    // 讓 AR 平面恢復「接收模型投影」的原始陰影方法。
    public static class RevertARPlaneShadows
    {
        const string ShadowReceiverGuid = "e18a21f2c7ffae44b89ce31f6d1d6d47";

        [MenuItem("Tools/Revert AR Plane Shadows (ShadowReceiver)")]
        public static void Apply()
        {
            string path = AssetDatabase.GUIDToAssetPath(ShadowReceiverGuid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogError("[RevertShadows] 找不到 ShadowReceiver.mat"); return; }

            int n = 0;
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (mb.GetType().Name != "SwitchARPlaneMaterial") continue;
                var so = new SerializedObject(mb);
                var mA = so.FindProperty("materialA");
                if (mA != null) { mA.objectReferenceValue = mat; so.ApplyModifiedProperties(); n++; }
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RevertShadows] materialA 設回 ShadowReceiver ({n} 個)，已存場景。AR 平面恢復接收影子。");
        }
    }
}
#endif
