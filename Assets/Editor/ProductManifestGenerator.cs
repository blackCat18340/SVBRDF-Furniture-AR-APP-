#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ARFurniture;
using UnityEditor;
using UnityEngine;

// 在編輯器掃 StreamingAssets/3D_Model，產生 manifest.json 供 runtime 讀取。
// 每次新增/修改產品資料夾後，跑一次 Tools/Generate Product Manifest。
public static class ProductManifestGenerator
{
    private const string RootFolderName = "3D_Model";
    private const string ModelFolderName = "3d_model";
    private const string ImageFolderName = "PICS";

    [MenuItem("Tools/Generate Product Manifest")]
    public static void Generate()
    {
        string root = Path.Combine(Application.streamingAssetsPath, RootFolderName);
        if (!Directory.Exists(root))
        {
            EditorUtility.DisplayDialog("Product Manifest",
                $"找不到資料夾:\n{root}\n\n請先把 3D_Model 放到 Assets/StreamingAssets/ 底下。", "OK");
            return;
        }

        ProductManifest manifest = new ProductManifest();

        foreach (string productFolder in Directory.GetDirectories(root))
        {
            ProductManifestEntry entry = BuildEntry(productFolder);
            if (entry != null) manifest.products.Add(entry);
        }

        string outPath = Path.Combine(root, "manifest.json");
        File.WriteAllText(outPath, JsonUtility.ToJson(manifest, true));
        AssetDatabase.Refresh();

        Debug.Log($"[ProductManifest] 已產生 {manifest.products.Count} 筆產品 → {outPath}");
        EditorUtility.DisplayDialog("Product Manifest",
            $"完成，共 {manifest.products.Count} 筆產品。\n{outPath}", "OK");
    }

    private static ProductManifestEntry BuildEntry(string productFolder)
    {
        string modelFolder = Path.Combine(productFolder, ModelFolderName);
        string[] modelFiles = Directory.Exists(modelFolder)
            ? Directory.GetFiles(modelFolder, "*.glb", SearchOption.TopDirectoryOnly)
            : Directory.GetFiles(productFolder, "*.glb", SearchOption.AllDirectories);

        if (modelFiles.Length == 0)
        {
            Debug.LogWarning($"[ProductManifest] 無 GLB，略過: {productFolder}");
            return null;
        }

        ProductManifestEntry entry = new ProductManifestEntry
        {
            id = Path.GetFileName(productFolder),
            productName = Path.GetFileName(productFolder).Replace('_', ' '),
            model = ToStreamingRelative(modelFiles[0])
        };

        ReadInfoFile(productFolder, entry);

        string imageFolder = Path.Combine(productFolder, ImageFolderName);
        if (!Directory.Exists(imageFolder)) imageFolder = productFolder;
        foreach (string img in Directory.GetFiles(imageFolder, "*.*", SearchOption.TopDirectoryOnly))
        {
            string ext = Path.GetExtension(img).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                entry.images.Add(ToStreamingRelative(img));
            }
        }

        return entry;
    }

    private static void ReadInfoFile(string productFolder, ProductManifestEntry entry)
    {
        string infoPath = Path.Combine(productFolder, "Info.json");
        if (!File.Exists(infoPath)) infoPath = Path.Combine(productFolder, "Info.txt");
        if (!File.Exists(infoPath)) return;

        string text = File.ReadAllText(infoPath).Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (text.StartsWith("{"))
        {
            LocalProductInfo info = JsonUtility.FromJson<LocalProductInfo>(text);
            if (info == null) return;
            if (!string.IsNullOrWhiteSpace(info.productName)) entry.productName = info.productName.Trim();
            if (info.price > 0f) entry.price = info.price;
            if (!string.IsNullOrWhiteSpace(info.url)) entry.url = info.url.Trim();
            if (!string.IsNullOrWhiteSpace(info.description)) entry.description = info.description.Trim();
            if (!string.IsNullOrWhiteSpace(info.sizeOptions)) entry.sizeOptions = info.sizeOptions.Trim();
            return;
        }

        string[] lines = text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])) entry.productName = lines[0].Trim();
        if (lines.Length > 1) float.TryParse(lines[1], NumberStyles.Float, CultureInfo.InvariantCulture, out entry.price);
        if (lines.Length > 2) entry.url = lines[2].Trim();
        if (lines.Length > 3) entry.description = lines[3].Trim();
        if (lines.Length > 4) entry.sizeOptions = lines[4].Trim();
    }

    private static string ToStreamingRelative(string fullPath)
    {
        string sa = Application.streamingAssetsPath.Replace('\\', '/');
        string p = fullPath.Replace('\\', '/');
        return p.StartsWith(sa) ? p.Substring(sa.Length).TrimStart('/') : p;
    }
}
#endif
