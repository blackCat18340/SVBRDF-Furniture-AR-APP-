using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// 性能監控器，顯示FPS、記憶體使用量和其他性能指標
/// </summary>
public class PerfMeter : MonoBehaviour
{
    [Header("顯示設定")]
    [Tooltip("是否顯示性能面板")]
    public bool showPerfPanel = true;

    [Tooltip("是否只在開發模式中顯示")]
    public bool showOnlyInDevMode = false;

    [Tooltip("是否顯示詳細資訊")]
    public bool showDetailedStats = true;

    [Tooltip("更新間隔")]
    public float updateInterval = 0.5f;

    [Header("顯示元件")]
    [Tooltip("FPS 文字元件")]
    public TextMeshProUGUI fpsText;

    [Tooltip("詳細統計資訊文字元件")]
    public TextMeshProUGUI statsText;

    [Tooltip("性能面板根物件")]
    public GameObject panelRoot;

    [Header("FPS 顏色設定")]
    public Color goodFpsColor = new Color(0.0f, 1.0f, 0.0f);
    public Color warningFpsColor = new Color(1.0f, 1.0f, 0.0f);
    public Color badFpsColor = new Color(1.0f, 0.0f, 0.0f);
    public int goodFpsThreshold = 55;
    public int warningFpsThreshold = 30;

    private float accum = 0.0f;
    private int frames = 0;
    private float timeLeft;
    private float fps;
    private float lastMemoryUsage;
    private string deviceInfo;

    private void Start()
    {
        timeLeft = updateInterval;

        // 快取設備資訊
        deviceInfo = $"Device: {SystemInfo.deviceModel}\nCPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)\nRAM: {SystemInfo.systemMemorySize} MB\nGPU: {SystemInfo.graphicsDeviceName}\nOS: {SystemInfo.operatingSystem}";

        // 如果只在開發模式顯示且當前非開發模式，則隱藏
        if (showOnlyInDevMode && !Debug.isDebugBuild)
        {
            showPerfPanel = false;
        }

        // 設置初始可見性
        if (panelRoot != null)
        {
            panelRoot.SetActive(showPerfPanel);
        }
    }

    private void Update()
    {
        // 只有當面板顯示時進行計算
        if (!showPerfPanel) return;

        // 計算FPS
        timeLeft -= Time.unscaledDeltaTime;
        accum += Time.unscaledDeltaTime;
        frames++;

        // 更新顯示
        if (timeLeft <= 0.0f)
        {
            fps = frames / accum;
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;

            UpdateStats();
        }
    }

    private void UpdateStats()
    {
        if (fpsText != null)
        {
            fpsText.text = $"{fps:F1} FPS";

            // 設定顏色
            if (fps >= goodFpsThreshold)
                fpsText.color = goodFpsColor;
            else if (fps >= warningFpsThreshold)
                fpsText.color = warningFpsColor;
            else
                fpsText.color = badFpsColor;
        }

        if (statsText != null && showDetailedStats)
        {
            // 獲取當前已用記憶體
#if UNITY_2018_1_OR_NEWER
            float currentMemoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
#else
            float currentMemoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
#endif
            float memoryDelta = currentMemoryUsage - lastMemoryUsage;
            lastMemoryUsage = currentMemoryUsage;

            // 獲取垃圾收集器資訊
            string gcInfo = $"GC: {System.GC.CollectionCount(0)}";

            // 顯示詳細統計資訊
            statsText.text = $"RAM: {currentMemoryUsage:F1} MB ({memoryDelta:F1} MB)\n";
            //{gcInfo}\nResolution: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRate}Hz
        }
    }

    public void ToggleVisibility()
    {
        showPerfPanel = !showPerfPanel;
        if (panelRoot != null)
        {
            panelRoot.SetActive(showPerfPanel);
        }
    }

    public void ToggleDetailedStats()
    {
        showDetailedStats = !showDetailedStats;
        if (statsText != null)
        {
            statsText.gameObject.SetActive(showDetailedStats);
        }
    }

    public void ShowDeviceInfo()
    {
        if (statsText != null)
        {
            statsText.text = deviceInfo;
            // 5秒後恢復正常資訊顯示
            Invoke("UpdateStats", 5.0f);
        }
    }
} 