using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 自動清理場景中的過時FPS顯示物件和腳本
/// </summary>
public class FPSCleanup : MonoBehaviour
{
    // 可能與我們的性能管理系統衝突的腳本名稱
    private readonly string[] conflictingScriptNames = new string[] {
        "ShowFPS",
        "FPSDisplay",
        "FPSCounter",
        "FrameRateCounter",
        "FpsMonitor",
        "FPSController",
        "FramerateManager",
        "TMP_FrameRateCounter",
        "TMP_UiFrameRateCounter"
    };

    private void Awake()
    {
        Debug.Log("FPSCleanup: 開始清理衝突的FPS顯示和控制腳本");
        
        // 尋找並禁用衝突的FPS物件
        CleanupFPSObjects();

        // 尋找並禁用衝突的FPS腳本
        CleanupFPSScripts();

        // 重置應用程式的目標幀率，以便我們的FrameRateManager可以控制它
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
    }

    private void CleanupFPSObjects()
    {
        // 查找名稱包含 "fps" 或 "FPS" 的物件
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            string lowerName = obj.name.ToLower();
            if (lowerName.Contains("showfps") || lowerName.Contains("fps") && !lowerName.Contains("cleanup"))
            {
                if (obj != this.gameObject && obj.transform.parent != this.transform.parent)
                {
                    Debug.LogWarning($"FPSCleanup: 禁用可能衝突的FPS物件: {obj.name}");
                    obj.SetActive(false);
                }
            }
        }
    }

    private void CleanupFPSScripts()
    {
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        if (allScripts != null)
        {
            foreach (MonoBehaviour script in allScripts)
            {
                if (script == null) continue;

                string scriptName = script.GetType().Name;
                if (conflictingScriptNames.Any(name => scriptName.Contains(name)))
                {
                    if (script.gameObject != this.gameObject)
                    {
                        Debug.LogWarning($"FPSCleanup: 禁用衝突的腳本: {scriptName} 在物件 {script.gameObject.name}");
                        script.enabled = false;
                    }
                }
            }
        }

        // 特別檢查AR相機上的幀率控制器
        Camera arCamera = FindObjectOfType<Camera>();
        if (arCamera != null)
        {
            MonoBehaviour[] cameraScripts = arCamera.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in cameraScripts)
            {
                if (script == null) continue;
                
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("fps") || scriptName.Contains("frame") || scriptName.Contains("rate"))
                {
                    Debug.LogWarning($"FPSCleanup: 在AR相機上禁用幀率控制腳本: {script.GetType().Name}");
                    script.enabled = false;
                }
            }
        }
    }
} 