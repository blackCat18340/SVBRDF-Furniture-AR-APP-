using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ARFurniture
{
    // 從 GitHub repo(PBR_3D) 取得產品資料：用 GitHub API 一次列出整個檔案樹，內容用 raw URL 下載並做本地快取。
    // 產品清單(tree) 與 Info.json 每次抓新的(才看得到新上架)；模型/圖片快取(大、少變)。
    public static class RemoteContent
    {
        public static string Owner = "TWAnnoyingKid";
        public static string Repo = "PBR_3D";
        public static string Branch = "main";
        public static string CacheVersion = "uncompressed-textures-v1";

        static string RawBase => $"https://raw.githubusercontent.com/{Owner}/{Repo}/{Branch}/";
        static string TreesUrl => $"https://api.github.com/repos/{Owner}/{Repo}/git/trees/{Branch}?recursive=1";
        static string CacheDir => Path.Combine(Application.persistentDataPath, "pbr_cache", CacheVersion);

        // === 對外資料結構 ===
        public class RepoProduct
        {
            public string id;
            public string infoPath;
            public string modelPath;
            public List<string> imagePaths = new List<string>();
        }

        // === URL ===
        public static string RawUrl(string repoPath)
        {
            var segs = repoPath.Replace('\\', '/').Split('/');
            for (int i = 0; i < segs.Length; i++) segs[i] = Uri.EscapeDataString(segs[i]);
            return RawBase + string.Join("/", segs);
        }

        // === 下載 ===
        public static async Task<byte[]> DownloadBytesAsync(string url)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", "PBR-AR-App"); // GitHub API 沒帶 UA 會被擋
                var tcs = new TaskCompletionSource<bool>();
                req.SendWebRequest().completed += _ => tcs.TrySetResult(true);
                await tcs.Task;
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[RemoteContent] 下載失敗 {req.responseCode}: {url} ({req.error})");
                    return null;
                }
                return req.downloadHandler.data;
            }
        }

        // repoPath 例如 "vase/PICS/1.webp"；cache=true 存本地、下次直接讀
        public static async Task<byte[]> GetContentAsync(string repoPath, bool cache)
        {
            string cachePath = CachePathFor(repoPath);
            if (cache && File.Exists(cachePath))
            {
                try { return File.ReadAllBytes(cachePath); } catch { }
            }
            byte[] data = await DownloadBytesAsync(RawUrl(repoPath));
            if (data != null && cache) WriteCache(cachePath, data);
            return data;
        }

        // 給 ModelLoader：傳完整 URL；http 就下載+快取，本地路徑就退回 StreamingAssetsIO
        public static async Task<byte[]> GetByUrlAsync(string url, bool cache)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return await StreamingAssetsIO.ReadAllBytesAsync(url); // 本地/StreamingAssets

            string cachePath = Path.Combine(CacheDir, "byurl", Hash(url) + ".bin");
            if (cache && File.Exists(cachePath))
            {
                try { return File.ReadAllBytes(cachePath); } catch { }
            }
            byte[] data = await DownloadBytesAsync(url);
            if (data != null && cache) WriteCache(cachePath, data);
            return data;
        }

        // === GitHub API：一次列出整個 repo 的檔案樹，分組成產品 ===
        public static async Task<List<RepoProduct>> ListProductsAsync()
        {
            var result = new List<RepoProduct>();
            string json = await DownloadTextAsync(TreesUrl);
            if (string.IsNullOrEmpty(json)) return result;

            TreeResp resp;
            try { resp = JsonUtility.FromJson<TreeResp>(json); }
            catch (Exception e) { Debug.LogError($"[RemoteContent] 解析 tree 失敗: {e.Message}"); return result; }
            if (resp?.tree == null) return result;
            if (resp.truncated) Debug.LogWarning("[RemoteContent] GitHub tree 被截斷(repo 太大)，部分產品可能漏掉");

            var map = new Dictionary<string, RepoProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in resp.tree)
            {
                if (it == null || it.type != "blob" || string.IsNullOrEmpty(it.path)) continue;
                var parts = it.path.Split('/');
                if (parts.Length < 2) continue;
                string id = parts[0];
                if (!map.TryGetValue(id, out var p)) { p = new RepoProduct { id = id }; map[id] = p; }

                string lower = it.path.ToLowerInvariant();
                if (parts.Length == 2 && parts[1].Equals("Info.json", StringComparison.OrdinalIgnoreCase))
                    p.infoPath = it.path;
                else if (parts.Length >= 3 && parts[1].Equals("3d_model", StringComparison.OrdinalIgnoreCase) && lower.EndsWith(".glb"))
                { if (p.modelPath == null) p.modelPath = it.path; }
                else if (parts.Length >= 3 && parts[1].Equals("PICS", StringComparison.OrdinalIgnoreCase) && IsImage(lower))
                    p.imagePaths.Add(it.path);
            }

            foreach (var p in map.Values)
                if (p.modelPath != null) result.Add(p); // 沒模型的略過
            result.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static async Task<string> DownloadTextAsync(string url)
        {
            byte[] b = await DownloadBytesAsync(url);
            return b == null ? null : Encoding.UTF8.GetString(b);
        }

        // === 內部 ===
        static bool IsImage(string lower) =>
            lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".webp");

        static string CachePathFor(string repoPath)
        {
            string safe = repoPath.Replace('\\', '/').Replace("..", "");
            return Path.Combine(CacheDir, safe.Replace('/', Path.DirectorySeparatorChar));
        }

        static void WriteCache(string cachePath, byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                File.WriteAllBytes(cachePath, data);
            }
            catch (Exception e) { Debug.LogWarning($"[RemoteContent] 寫快取失敗: {e.Message}"); }
        }

        static string Hash(string s)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        [Serializable] class TreeResp { public TreeItem[] tree; public bool truncated; }
        [Serializable] class TreeItem { public string path; public string type; }
    }
}
