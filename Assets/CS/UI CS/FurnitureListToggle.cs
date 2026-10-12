using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class FurnitureListToggle : MonoBehaviour
{
    // 單例模式，方便其他地方存取此控制器
    public static FurnitureListToggle Instance { get; private set; }

    // 家具列表面板的 RectTransform (請從 Canvas 中拖入對應的物件)
    public RectTransform furnitureListPanel;

    // 是否正在顯示家具列表面板
    private bool isShowing = false;

    // 面板動畫持續時間（秒）
    public float animationDuration = 0.3f;

    // 用於面板滑動動畫的曲線 (可在 Inspector 中設定)
    public AnimationCurve slideCurve;

    // 按鈕控制 (用於切換箭頭圖示)
    public Button myButton;
    public Sprite upSprite;    // 向上箭頭圖示
    public Sprite downSprite;  // 向下箭頭圖示

    // 以下參數請在 Inspector 中設定：
    // 隱藏時的面板位置 (座標值)
    public Vector2 hiddenPos;
    // 顯示時的面板位置 (座標值)
    public Vector2 showPos;

    public TMP_Text deviceText;

    void Start()
    {
        // 檢查裝置類型是否為手持設備
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            // 取得螢幕 DPI
            float dpi = Screen.dpi;

            // 若 DPI 不可用，可能要以解析度來做近似判斷（這裡先假設為手機）
            if (dpi == 0)
            {
                Debug.Log("無法取得 DPI，預設判斷為手機。");
                furnitureListPanel.localScale *= 1.2f;
                hiddenPos = new Vector2(0, -844);
                showPos = new Vector2(0, 674);
                return;
            }

            // 計算螢幕寬、高的英吋數
            float screenWidthInches = Screen.width / dpi;
            float screenHeightInches = Screen.height / dpi;
            
            // 計算對角線長度（英吋）
            double diagonalInches = Math.Sqrt(Math.Pow(screenWidthInches, 2) + Math.Pow(screenHeightInches, 2));

            // 這裡以 7 吋為判斷門檻（依需求可調整）
            if (diagonalInches >= 7.0)
            {
                deviceText.text = "Tablet";
                furnitureListPanel.localScale *= 1f;
                hiddenPos = new Vector2(0, -696);
                showPos = new Vector2(0, 563);
                Debug.Log("目前裝置判斷為平板，對角線長度約：" + diagonalInches.ToString("F1") + " 吋");
            }
            else
            {
                deviceText.text = "Phone";
                furnitureListPanel.localScale *= 1.2f;
                hiddenPos = new Vector2(0, -844);
                showPos = new Vector2(0, 674);
                Debug.Log("目前裝置判斷為手機，對角線長度約：" + diagonalInches.ToString("F1") + " 吋");
            }
            furnitureListPanel.anchoredPosition = hiddenPos;
        }
        else
        {
            furnitureListPanel.localScale *= 1f;
            hiddenPos = new Vector2(0, -696);
            showPos = new Vector2(0, 563);
            furnitureListPanel.anchoredPosition = hiddenPos;
            Debug.Log("非手持設備，可能為桌面或其他類型裝置。");
        }
    }
    void Awake()
    {
        // 單例模式設定
        if (Instance == null)
        {
            Instance = this;
            // 切換場景時不銷毀此物件
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 切換按鈕上顯示的箭頭圖示
    public void OnClickToggleImage()
    {
        // 取得目前按鈕上顯示的 sprite
        Sprite currentSprite = myButton.image.sprite;

        // 如果目前為 upSprite，就切換為 downSprite；反之亦然
        if (currentSprite == upSprite)
        {
            myButton.image.sprite = downSprite;
        }
        else
        {
            myButton.image.sprite = upSprite;
        }
    }

    // 強制讓面板滑入 (顯示面板)
    public void ForceSlideIn()
    {
        if (!isShowing)
        {
            isShowing = true;
            furnitureListPanel.gameObject.SetActive(true);
            StartCoroutine(SlideIn(furnitureListPanel));
        }
    }

    // 強制讓面板滑出 (隱藏面板)
    public void ForceSlideOut()
    {
        if (isShowing)
        {
            isShowing = false;
            StartCoroutine(SlideOut(furnitureListPanel));
        }
    }

    // 點擊按鈕切換家具列表的顯示狀態
    public void OnClickToggleFurnitureList()
    {
        isShowing = !isShowing;

        if (isShowing)
        {
            // 顯示面板並執行滑入動畫
            furnitureListPanel.gameObject.SetActive(true);
            StartCoroutine(SlideIn(furnitureListPanel));
        }
        else
        {
            // 執行滑出動畫
            StartCoroutine(SlideOut(furnitureListPanel));
        }
    }

    // 面板滑入動畫 Coroutine
    public IEnumerator SlideIn(RectTransform target)
    {
        // 切換按鈕圖示
        OnClickToggleImage();
        float time = 0f;
        // 以目前面板位置作為起點 (通常是 hiddenPos)
        Vector2 startPos = target.anchoredPosition;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            float curveValue = slideCurve.Evaluate(t);
            
            // 在隱藏位置與顯示位置之間插值
            Vector2 newPos = Vector2.Lerp(hiddenPos, showPos, curveValue);
            target.anchoredPosition = newPos;

            yield return null;
        }
        // 最後確保面板位置設為顯示位置
        target.anchoredPosition = showPos;



        // float time = 0f;
        // // 取得 Canvas 的高度 (若父物件為 Canvas 的 RectTransform)
        // RectTransform parentRect = target.parent as RectTransform;
        // float finalY = parentRect != null ? parentRect.rect.height * 0.2f : Screen.height * 0.2f;

        // // 最終顯示位置，X 軸保持原來的隱藏位置 X 值，Y 軸固定為 Canvas 高度 80%
        // Vector2 finalPos = new Vector2(hiddenPos.x, finalY);

        // // 初始位置設為隱藏位置
        // target.anchoredPosition = hiddenPos;

        // // 插值動畫，依 slideCurve 曲線
        // while (time < animationDuration)
        // {
        //     time += Time.deltaTime;
        //     float t = time / animationDuration;
        //     float curveValue = slideCurve.Evaluate(t);
        //     // 在隱藏位置與最終顯示位置之間插值
        //     Vector2 newPos = Vector2.Lerp(hiddenPos, finalPos, curveValue);
        //     target.anchoredPosition = newPos;
        //     yield return null;
        // }

        // // 最後確保面板停留在最終位置
        // target.anchoredPosition = finalPos;
    }


    // 面板滑出動畫 Coroutine
    public IEnumerator SlideOut(RectTransform target)
    {
        // 切換按鈕圖示
        OnClickToggleImage();
        float time = 0f;
        // 以目前面板位置作為起點 (通常是 showPos)
        Vector2 startPos = target.anchoredPosition;

        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            float curveValue = slideCurve.Evaluate(t);

            // 在顯示位置與隱藏位置之間插值
            Vector2 newPos = Vector2.Lerp(showPos, hiddenPos, curveValue);
            target.anchoredPosition = newPos;

            yield return null;
        }
        // 最後確保面板位置設為隱藏位置
        target.anchoredPosition = hiddenPos;
        // 如果需要，也可以在此隱藏面板物件 (目前保留為啟用狀態)
        // target.gameObject.SetActive(false);
    }
}
