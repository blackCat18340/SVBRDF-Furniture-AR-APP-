using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ARFurniture
{
    // 跨平台讀取 StreamingAssets 內的檔案。
    // Android 的 StreamingAssets 在 APK 內、是壓縮的，不能用 File.IO，只能透過 UnityWebRequest。
    public static class StreamingAssetsIO
    {
        public static string Root => Application.streamingAssetsPath;

        // relativePath 例如 "3D_Model/SOFA/3d_model/pbr_full_multi.glb"
        public static string FullPath(string relativePath)
        {
            string root = Application.streamingAssetsPath.Replace('\\', '/').TrimEnd('/');
            return root + "/" + relativePath.Replace('\\', '/').TrimStart('/');
        }

        public static async Task<byte[]> ReadAllBytesAsync(string fullOrRelativePath)
        {
            string path = fullOrRelativePath.Replace('\\', '/');
            // 已是 streamingAssets 下的完整路徑就直接用，否則當成相對路徑
            if (!path.StartsWith(Application.streamingAssetsPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)
                && !path.Contains("://"))
            {
                path = FullPath(path);
            }

            // Android 的 path 已是 jar:file://... ；其他平台是一般絕對路徑，轉成正確的 file:/// URI
            string uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (UnityWebRequest req = UnityWebRequest.Get(uri))
            {
                var tcs = new TaskCompletionSource<bool>();
                var op = req.SendWebRequest();
                op.completed += _ => tcs.TrySetResult(true);
                await tcs.Task;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[StreamingAssetsIO] 讀取失敗: {uri} ({req.error})");
                    return null;
                }
                return req.downloadHandler.data;
            }
        }

        public static async Task<string> ReadAllTextAsync(string fullOrRelativePath)
        {
            byte[] bytes = await ReadAllBytesAsync(fullOrRelativePath);
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }
    }
}
