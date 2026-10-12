using System.Collections.Generic;

namespace ARFurniture
{
    // StreamingAssets/3D_Model/manifest.json 的結構。
    // 由 Editor 的「Tools/Generate Product Manifest」在 edit 時掃資料夾產生，
    // runtime 直接讀這份清單，不再掃資料夾（Android 不能掃）。
    [System.Serializable]
    public class ProductManifest
    {
        public List<ProductManifestEntry> products = new List<ProductManifestEntry>();
    }

    [System.Serializable]
    public class ProductManifestEntry
    {
        public string id;
        public string model;              // 相對 StreamingAssets 的路徑，如 3D_Model/SOFA/3d_model/x.glb
        public List<string> images = new List<string>();
        public string productName;
        public float price;
        public string url;
        public string description;
        public string sizeOptions;
    }
}
