#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARFurniture.EditorTools
{
    // 一鍵：把 CameraLightEstimation 裝進場景(自動找 AR 相機 + 方向光)。不動反射、不動其他設定。
    public static class AddCameraLightEstimation
    {
        [MenuItem("Tools/Add Camera Light Estimation")]
        public static void Apply()
        {
            var existing = Object.FindObjectOfType<ARFurniture.CameraLightEstimation>();
            if (existing == null)
            {
                var go = new GameObject("Camera Light Estimation");
                go.AddComponent<ARFurniture.CameraLightEstimation>();
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CameraLightEstimation] 已裝進場景。需真機測(讀相機影像)。亮度/色彩不對就調該元件的參數。");
        }
    }
}
#endif
