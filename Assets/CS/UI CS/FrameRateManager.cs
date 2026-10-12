using UnityEngine;

namespace ARFurniture
{
    /// <summary>
    /// 全局幀率管理器，確保應用在所有平台上以最佳性能運行
    /// </summary>
    public class FrameRateManager : MonoBehaviour
    {
        [Tooltip("目標幀率 (60+ 為高性能模式)")]
        [SerializeField] private int targetFrameRate = 60;
        
        [Tooltip("是否禁用垂直同步")]
        [SerializeField] private bool disableVSync = true;
        
        [Tooltip("是否以高性能模式運行 (防止系統降低性能)")]
        [SerializeField] private bool highPerformanceMode = true;

        private void Awake()
        {
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }
            
            Application.targetFrameRate = targetFrameRate;
            
            // 在 Android 上，請求高性能模式
            if (highPerformanceMode && Application.platform == RuntimePlatform.Android)
            {
                RequestHighPerformanceMode();
            }
            
            // 設為單例，確保只有一個實例在運行
            DontDestroyOnLoad(this.gameObject);
            
            Debug.Log($"幀率管理器初始化: 目標幀率 = {targetFrameRate}, vSync = {(disableVSync ? "禁用" : "啟用")}, 高性能模式 = {(highPerformanceMode ? "啟用" : "禁用")}");
        }
        
        private void RequestHighPerformanceMode()
        {
            // 防止 CPU 和 GPU 節流
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                    {
                        window.Call("addFlags", 0x00080000); // FLAG_KEEP_SCREEN_ON
                        
                        try
                        {
                            using (AndroidJavaObject windowParams = window.Call<AndroidJavaObject>("getAttributes"))
                            {
                                // 嘗試設置 LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES 減少 UI 切換時的卡頓
                                windowParams.Call("setLayoutInDisplayCutoutMode", 1);
                                window.Call("setAttributes", windowParams);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("設置 Android 高性能模式時出錯: " + e.Message);
                        }
                    }
                }
            }
        }
    }
} 