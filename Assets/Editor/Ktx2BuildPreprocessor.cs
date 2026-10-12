#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARFurniture.EditorTools
{
    // Build 前自動把 StreamingAssets 裡的所有 GLB 貼圖轉成 KTX2/UASTC。
    // 手機上 glTFast 會把它 transcode 成 ASTC（顯存/頻寬 ~6-8x，解析度維持 4K）。
    // 不用手動、不用一個一個模型做；已轉過(含 KHR_texture_basisu)的自動跳過，可重複 build。
    // 需求：開發機裝過 gltf-transform（npm i -g @gltf-transform/cli）。沒裝就沿用未壓縮，不擋 build。
    public class Ktx2BuildPreprocessor : IPreprocessBuildWithReport
    {
        private const bool EnableAutomaticKtx2Conversion = false;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!EnableAutomaticKtx2Conversion)
            {
                UnityEngine.Debug.Log("[KTX2] Automatic KTX2 conversion is disabled. Using original GLB textures.");
                return;
            }

            string root = Path.Combine(Application.streamingAssetsPath, "3D_Model");
            if (!Directory.Exists(root)) return;

            foreach (string glb in Directory.GetFiles(root, "*.glb", SearchOption.AllDirectories))
            {
                if (AlreadyBasis(glb)) continue;
                UnityEngine.Debug.Log($"[KTX2] 轉檔中: {glb}");
                if (RunGltfTransform(glb))
                    UnityEngine.Debug.Log($"[KTX2] 完成: {glb}");
                else
                    UnityEngine.Debug.LogWarning($"[KTX2] 轉檔失敗或缺 gltf-transform，沿用未壓縮貼圖: {glb}");
            }
            AssetDatabase.Refresh();
        }

        static bool AlreadyBasis(string path)
        {
            // GLB 的 JSON chunk 是純文字，直接找擴充名字串即可判斷有沒有壓過。
            // ponytail: 整檔讀進來比對；build 不常跑、4K GLB 也就幾十 MB，夠用。
            string ascii = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path));
            return ascii.Contains("KHR_texture_basisu");
        }

        static bool RunGltfTransform(string glb)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    // Windows 上 gltf-transform 是 .cmd，透過 cmd.exe 才吃得到 PATH。
                    FileName = "cmd.exe",
                    Arguments = $"/c gltf-transform uastc \"{glb}\" \"{glb}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode != 0 && !string.IsNullOrEmpty(err))
                        UnityEngine.Debug.LogWarning($"[KTX2] gltf-transform: {err}");
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }
    }
}
#endif
