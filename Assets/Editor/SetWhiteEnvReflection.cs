#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARFurniture.EditorTools
{
    public static class SetWhiteEnvReflection
    {
        const string ExrPath = "Assets/Shaders/monochrome_studio_02_4k.exr";
        const float ReflectionIntensity = 1.0f; // 卷積修好後，粗糙布料會自己變霧，不用靠壓強度

        [MenuItem("Tools/Set White Environment Reflection")]
        public static void Apply()
        {
            var importer = AssetImporter.GetAtPath(ExrPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[EnvReflection] 找不到或無法匯入: " + ExrPath);
                return;
            }

            // 確保以 Cubemap(equirect) 匯入 + mipmap + Specular 卷積
            var st = new TextureImporterSettings();
            importer.ReadTextureSettings(st);
            bool changed = false;
            if (st.textureShape != TextureImporterShape.TextureCube) { st.textureShape = TextureImporterShape.TextureCube; changed = true; }
            if (st.generateCubemap != TextureImporterGenerateCubemap.Cylindrical) { st.generateCubemap = TextureImporterGenerateCubemap.Cylindrical; changed = true; }
            if (!st.mipmapEnabled) { st.mipmapEnabled = true; changed = true; }
            // 關鍵：Specular 卷積 → Unity 依粗糙度把 mip 糊化(GGX)，粗糙表面才會反到糊化變暗的環境(跟 Blender 一樣)。
            // 沒這個，原始 cubemap 對所有粗糙度都給清晰反射 → 布料過亮。
            if (st.cubemapConvolution != TextureImporterCubemapConvolution.Specular) { st.cubemapConvolution = TextureImporterCubemapConvolution.Specular; changed = true; }
            if (changed) { importer.SetTextureSettings(st); importer.SaveAndReimport(); }

            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(ExrPath);
            if (cube == null)
            {
                Debug.LogError("[EnvReflection] 設成 Cubemap 後仍讀不到 Cubemap，檢查 EXR 是否為 equirect 全景圖。");
                return;
            }

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = ReflectionIntensity;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[EnvReflection] 完成：環境反射 = {ExrPath} (Cubemap, Intensity={ReflectionIntensity})，已存場景。");
        }
    }
}
#endif
