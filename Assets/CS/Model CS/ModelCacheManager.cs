using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;

public class ModelCacheManager : MonoBehaviour
    {
        private const int MAX_CACHED_MODELS = 10;
        private const string CACHE_FOLDER = "ModelCache";
        private const string CACHE_INFO_FILE = "cache_info.json";
        
        private string cachePath;
        private CacheInfo cacheInfo;
        
        [System.Serializable]
        private class CacheInfo
        {
            public List<CachedModelInfo> models = new List<CachedModelInfo>();
        }
        
        [System.Serializable]
        private class CachedModelInfo
        {
            public string modelUrl;
            public string localPath;
            public string productJson;
            public long timestamp;
        }
        
        private void Awake()
        {
            cachePath = Path.Combine(Application.persistentDataPath, CACHE_FOLDER);
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            LoadCacheInfo();
        }
        
        private void LoadCacheInfo()
        {
            string infoPath = Path.Combine(cachePath, CACHE_INFO_FILE);
            if (File.Exists(infoPath))
            {
                string json = File.ReadAllText(infoPath);
                cacheInfo = JsonUtility.FromJson<CacheInfo>(json);
            }
            else
            {
                cacheInfo = new CacheInfo();
            }
        }
        
        private void SaveCacheInfo()
        {
            string infoPath = Path.Combine(cachePath, CACHE_INFO_FILE);
            string json = JsonUtility.ToJson(cacheInfo);
            File.WriteAllText(infoPath, json);
        }
        
        public bool TryLoadFromCache(string modelUrl, out byte[] modelData, out string productJson)
        {
            modelData = null;
            productJson = null;
            
            var cachedModel = cacheInfo.models.FirstOrDefault(m => m.modelUrl == modelUrl);
            if (cachedModel != null)
            {
                string modelPath = Path.Combine(cachePath, cachedModel.localPath);
                if (File.Exists(modelPath))
                {
                    modelData = File.ReadAllBytes(modelPath);
                    productJson = cachedModel.productJson;
                    // 更新時間戳
                    cachedModel.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    SaveCacheInfo();
                    return true;
                }
            }
            return false;
        }
        
        public async Task CacheModel(string modelUrl, byte[] modelData, string productJson)
        {
            // 檢查是否已存在
            var existingModel = cacheInfo.models.FirstOrDefault(m => m.modelUrl == modelUrl);
            if (existingModel != null)
            {
                // 更新現有緩存
                existingModel.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                existingModel.productJson = productJson;
                string modelPath = Path.Combine(cachePath, existingModel.localPath);
                File.WriteAllBytes(modelPath, modelData);
            }
            else
            {
                // 檢查是否需要清理舊緩存
                if (cacheInfo.models.Count >= MAX_CACHED_MODELS)
                {
                    // 按時間戳排序並移除最舊的
                    var oldestModel = cacheInfo.models.OrderBy(m => m.timestamp).First();
                    string oldModelPath = Path.Combine(cachePath, oldestModel.localPath);
                    if (File.Exists(oldModelPath))
                    {
                        File.Delete(oldModelPath);
                    }
                    cacheInfo.models.Remove(oldestModel);
                }
                
                // 添加新緩存
                string fileName = System.Guid.NewGuid().ToString() + ".glb";
                string modelPath = Path.Combine(cachePath, fileName);
                File.WriteAllBytes(modelPath, modelData);
                
                cacheInfo.models.Add(new CachedModelInfo
                {
                    modelUrl = modelUrl,
                    localPath = fileName,
                    productJson = productJson,
                    timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
            
            SaveCacheInfo();
        }
        
        public void ClearCache()
        {
            foreach (var model in cacheInfo.models)
            {
                string modelPath = Path.Combine(cachePath, model.localPath);
                if (File.Exists(modelPath))
                {
                    File.Delete(modelPath);
                }
            }
            cacheInfo.models.Clear();
            SaveCacheInfo();
        }
    }