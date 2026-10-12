#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ARFurniture.EditorTools
{
    // 一鍵：把投影陰影用的兩個 shader 加進 Always Included Shaders，避免 build 被 strip。
    // (它們是 runtime 用 Shader.Find 建材質的，沒被任何 .mat 引用 → 不加就會在裝置上找不到 → 沒影子)
    public static class AddProjectedShadowShaders
    {
        static readonly string[] Names = { "Custom/ShadowSilhouette", "Custom/ProjectedShadowQuad" };

        [MenuItem("Tools/Add Projected Shadow Shaders to Always Included")]
        public static void Apply()
        {
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) { Debug.LogError("[Shaders] 讀不到 GraphicsSettings"); return; }
            var so = new SerializedObject(gs[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");

            int added = 0;
            foreach (var name in Names)
            {
                var sh = Shader.Find(name);
                if (sh == null) { Debug.LogError($"[Shaders] 找不到 shader: {name}（檔案有編譯成功嗎?）"); continue; }

                bool exists = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { exists = true; break; }

                if (!exists)
                {
                    arr.InsertArrayElementAtIndex(arr.arraySize);
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                    added++;
                    Debug.Log($"[Shaders] 已加入 Always Included: {name}");
                }
                else Debug.Log($"[Shaders] 已存在: {name}");
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Shaders] 完成，新增 {added} 個。重新 build 即可。");
        }
    }
}
#endif
