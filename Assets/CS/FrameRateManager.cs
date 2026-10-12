using UnityEngine;

/// <summary>
/// 幀率管理器，控制應用程式的幀率和性能設定
/// </summary>
public class FrameRateManager : MonoBehaviour
{
    [Tooltip("目標幀率，設為-1表示不限制")]
    public int targetFrameRate = 60;

    [Tooltip("是否禁用垂直同步")]
    public bool disableVSync = true;

    [Tooltip("是否啟用高性能模式")]
    public bool highPerformanceMode = true;

    private void Awake()
    {
        SetupFrameRate();
    }

    private void OnEnable()
    {
        // 確保設定在啟用時刷新
        SetupFrameRate();
    }

    /// <summary>
    /// 設置應用程式的幀率和相關設定
    /// </summary>
    private void SetupFrameRate()
    {
        if (disableVSync)
        {
            // 禁用垂直同步
            QualitySettings.vSyncCount = 0;
        }

        // 設置目標幀率
        Application.targetFrameRate = targetFrameRate;

        // 如果啟用高性能模式，設置適當的性能配置
        if (highPerformanceMode)
        {
            // 避免CPU休眠
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 降低固定更新頻率以減少CPU使用
            Time.fixedDeltaTime = 0.02f;

            // 使用降採樣來增加效能（僅在移動設備上）
            if (Application.isMobilePlatform)
            {
                Resolution currentResolution = Screen.currentResolution;
                int targetWidth = Mathf.RoundToInt(currentResolution.width * 0.8f);
                int targetHeight = Mathf.RoundToInt(currentResolution.height * 0.8f);
                
                // 在某些設備上可能不支持分辨率變更
                try
                {
                    Screen.SetResolution(targetWidth, targetHeight, true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("無法變更螢幕分辨率: " + e.Message);
                }
            }
        }

        Debug.Log($"FrameRateManager: 幀率設定為 {targetFrameRate}, VSync: {(disableVSync ? "禁用" : "啟用")}, 高性能模式: {(highPerformanceMode ? "啟用" : "禁用")}");
    }

    /// <summary>
    /// 調整目標幀率
    /// </summary>
    public void SetTargetFrameRate(int newFrameRate)
    {
        targetFrameRate = newFrameRate;
        Application.targetFrameRate = targetFrameRate;
        Debug.Log($"FrameRateManager: 目標幀率已調整為 {targetFrameRate}");
    }

    /// <summary>
    /// 切換高性能模式
    /// </summary>
    public void ToggleHighPerformanceMode(bool enable)
    {
        highPerformanceMode = enable;
        SetupFrameRate();
    }
} 