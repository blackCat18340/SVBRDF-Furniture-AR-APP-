using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems;

public class InfoPanelController : MonoBehaviour
{
    public static InfoPanelController Instance;
    [Header("商品瀏覽UI")]
    public Button UIbtn;

    [Header("Panel Root")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Arrows")]
    public Button leftArrow;
    public Button rightArrow;

    [Header("ScrollRect Components")]
    public ScrollRect mainImageScrollRect;
    public Transform mainImageContent;
    public GameObject imageItemPrefab;
    public ScrollRect smallImageScrollRect;
    public Transform smallImageContent;
    public GameObject smallImageItemPrefab;

    [Header("Text Info")]
    public Text nameText;
    public TMP_Text priceText;
    public Button urlButton;
    public Text otherInfoText;
    public Text sizeText; // 顯示產品尺寸

    [Header("購買數量與加入購物車")]
    public int buyingNum;
    public InputField buyingNumInput;
    public Button buyingNumPlus;
    public Button buyingNumMinus;
    public Button addInCart;

    // 物件池管理
    private Queue<GameObject> mainImagePool = new Queue<GameObject>();
    private Queue<GameObject> smallImagePool = new Queue<GameObject>();

    private List<Sprite> currentSprites;
    public int currentIndex = 0;
    private float totalContentWidth;

    void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);

        leftArrow.onClick.AddListener(OnClickLeft);
        rightArrow.onClick.AddListener(OnClickRight);

        buyingNum = 1;
        buyingNumPlus.onClick.AddListener(() => { Plus(); });
        buyingNumMinus.onClick.AddListener(() => { Minus(); });
    }

    public void ShowProductInfo(ProductData pd)
    {
        ProductListManager.Instance.OnClicklist();
        panelRoot.SetActive(true);
        InitializeUI(pd);
        GenerateImages(pd.allSprites);
        UpdateThumbnailAlpha();
        UpdateArrows();
        MoveToIndex(0);
    }

    private void InitializeUI(ProductData pd)
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => {
            buyingNum = 1;
            buyingNumInput.text = "1";
            Hide(pd.from);
        });

        currentIndex = 0;
        nameText.text = pd.productName;
        priceText.text = "$ " + pd.price;
        urlButton.onClick.RemoveAllListeners();
        urlButton.onClick.AddListener(() => OnUrlButtonClick(pd.url));
        otherInfoText.text = pd.otherInfo;
        if (sizeText != null)
        {
            // 重新解析尺寸資訊並按照 高 寬 深 的順序顯示
            string sizeInfo = pd.sizeOptions;
            if (sizeInfo.Contains("尺寸資訊不完整"))
            {
                sizeText.text = "尺寸: " + sizeInfo;
            }
            else
            {
                // 解析現有的尺寸資訊
                string heightInfo = "";
                string widthInfo = "";
                string depthInfo = "";
                
                if (sizeInfo.Contains("高"))
                {
                    int heightIndex = sizeInfo.IndexOf("高");
                    int nextIndex = sizeInfo.IndexOf(" x ", heightIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.IndexOf(" (cm)", heightIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.Length;
                    heightInfo = sizeInfo.Substring(heightIndex, nextIndex - heightIndex).Trim();
                }
                
                if (sizeInfo.Contains("寬"))
                {
                    int widthIndex = sizeInfo.IndexOf("寬");
                    int nextIndex = sizeInfo.IndexOf(" x ", widthIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.IndexOf(" (cm)", widthIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.Length;
                    widthInfo = sizeInfo.Substring(widthIndex, nextIndex - widthIndex).Trim();
                }
                
                if (sizeInfo.Contains("深"))
                {
                    int depthIndex = sizeInfo.IndexOf("深");
                    int nextIndex = sizeInfo.IndexOf(" x ", depthIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.IndexOf(" (cm)", depthIndex);
                    if (nextIndex == -1) nextIndex = sizeInfo.Length;
                    depthInfo = sizeInfo.Substring(depthIndex, nextIndex - depthIndex).Trim();
                }
                
                // 按照 高 寬 深 的順序重新組合
                List<string> orderedSizes = new List<string>();
                if (!string.IsNullOrEmpty(heightInfo)) orderedSizes.Add(heightInfo);
                if (!string.IsNullOrEmpty(widthInfo)) orderedSizes.Add(widthInfo);
                if (!string.IsNullOrEmpty(depthInfo)) orderedSizes.Add(depthInfo);
                
                string reorderedSize = orderedSizes.Count > 0 ? string.Join(" x ", orderedSizes) + " (cm)" : "尺寸資訊不完整";
                sizeText.text = "尺寸: " + reorderedSize;
            }
        }

        addInCart.onClick.RemoveAllListeners();
        addInCart.onClick.AddListener(() => { AddCart(pd.productName); });

        ClearOldImages();
        currentSprites = pd.allSprites ?? new List<Sprite>();
    }

    private void Plus()
    {
        buyingNum += 1;
        buyingNumInput.text = buyingNum.ToString();
    }
    private void Minus()
    {
        if (buyingNum - 1 > 0)
        {
            buyingNum -= 1;
            buyingNumInput.text = buyingNum.ToString();
        }
    }
    private void AddCart(string product)
    {
        Debug.Log($"加入購物車: {buyingNumInput.text} 個 {product}");
    }

    private void OnUrlButtonClick(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Debug.Log($"開啟連結: {url}");
#if UNITY_WEBGL
        Application.ExternalEval($"window.open('{EscapeUrl(url)}','_blank')");
#else
        Application.OpenURL(url);
#endif
    }

    private string EscapeUrl(string url)
    {
        return System.Uri.EscapeUriString(url);
    }

    private void GenerateImages(List<Sprite> sprites)
    {
        totalContentWidth = 0;
        HorizontalLayoutGroup layout = mainImageContent.GetComponent<HorizontalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 0;

        for (int i = 0; i < sprites.Count; i++)
        {
            GameObject mainImgGO = GetPooledObject(mainImagePool, imageItemPrefab, mainImageContent);// 主圖生成（使用物件池，生成的 prefab 為 Button，內部有 "img" 子物件）
            Transform imgTransform = mainImgGO.transform.Find("Image");// 取得 prefab 內部的 "img" 子物件
            if (imgTransform != null)
            {
                Image imgComponent = imgTransform.GetComponent<Image>();
                Sprite currentSprite = sprites[i];
                imgComponent.sprite = currentSprite;
                
                float fixedSize = 600f; //設定動態調整尺寸
                float spriteWidth = currentSprite.rect.width; //原始圖像尺寸
                float spriteHeight = currentSprite.rect.height; 
                float calculatedWidth = fixedSize * (spriteWidth / spriteHeight); //計算調整後寬
                float calculatedHeight = fixedSize * (spriteHeight / spriteWidth); //計算調整後高

                // 更新內部 img 的尺寸
                RectTransform imgRect = imgComponent.GetComponent<RectTransform>();
                // 圖像為 寬x高
                if(calculatedWidth > 600f){ //當寬度>600，調整圖像為600x調整後高
                    imgRect.sizeDelta = new Vector2(fixedSize, calculatedHeight);
                }
                else{ //反之調整圖像為調整後寬x600
                    imgRect.sizeDelta = new Vector2(calculatedWidth, fixedSize);
                }
                

                // 同時更新外層 Button 的 RectTransform（假設需要保持一致）
                RectTransform btnRect = mainImgGO.GetComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(600f, 600f);

                totalContentWidth += 600f + spacing;
            }
        }

        
        for (int i = 0; i < sprites.Count; i++)// 縮略圖生成（含互動）－保持原邏輯不變
        {
            GameObject smallImgGO = GetPooledObject(smallImagePool, smallImageItemPrefab, smallImageContent); // 使用小圖 prefab，同樣為 Button 且內部含有 "Image" 子物件
            RectTransform smallBtnRect = smallImgGO.GetComponent<RectTransform>();// 固定按鈕尺寸為 130x130
            smallBtnRect.sizeDelta = new Vector2(130f, 130f);
            
            Transform smallImgTransform = smallImgGO.transform.Find("Image"); // 取得按鈕內部 "Image" 子物件
            if (smallImgTransform != null)
            {
                Image smallImgComponent = smallImgTransform.GetComponent<Image>();
                Sprite currentSprite = sprites[i];
                smallImgComponent.sprite = currentSprite;
                
                float fixedSize = 130f;
                float spriteWidth = currentSprite.rect.width;
                float spriteHeight = currentSprite.rect.height;
                float calculatedWidth = fixedSize * (spriteWidth / spriteHeight);
                float calculatedHeight = fixedSize * (spriteHeight / spriteWidth);
                
                RectTransform smallImgRect = smallImgComponent.GetComponent<RectTransform>();
                if (calculatedWidth > fixedSize)
                {
                    smallImgRect.sizeDelta = new Vector2(fixedSize, calculatedHeight);
                }
                else
                {
                    smallImgRect.sizeDelta = new Vector2(calculatedWidth, fixedSize);
                }
                smallImgRect.anchoredPosition = Vector2.zero;
            }
            
            // 設定按鈕點擊事件：點擊後呼叫 OnThumbnailClicked(index)
            Button smallBtnComponent = smallImgGO.GetComponent<Button>();
            int index = i;
            smallBtnComponent.onClick.RemoveAllListeners();
            smallBtnComponent.onClick.AddListener(() => OnThumbnailClicked(index));
            
            // 為保持縮略圖的排列順序，設定 sibling index
            smallImgGO.transform.SetSiblingIndex(i);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(smallImageContent as RectTransform);
        // 移除最後一個間距
        totalContentWidth -= spacing;
    }



    private GameObject GetPooledObject(Queue<GameObject> pool, GameObject prefab, Transform parent)
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetParent(parent);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, parent);
        }
        return obj;
    }

    private void ClearOldImages()
    {
        RecycleObjects(mainImageContent, mainImagePool);
        RecycleObjects(smallImageContent, smallImagePool);
    }

    private void RecycleObjects(Transform content, Queue<GameObject> pool)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject obj = content.GetChild(i).gameObject;
            obj.SetActive(false);
            pool.Enqueue(obj);
            obj.transform.SetParent(null);
        }
    }

    private void OnThumbnailClicked(int index)
    {
        currentIndex = index;
        UpdateArrows();
        MoveToIndex(currentIndex);
        UpdateThumbnailAlpha(); // 更新所有縮略圖的透明度
    }

    private void UpdateArrows()
    {
        leftArrow.gameObject.SetActive(currentIndex > 0);
        rightArrow.gameObject.SetActive(currentIndex < currentSprites.Count - 1);
    }

    private void OnClickLeft() => ChangeIndex(-1);
    private void OnClickRight() => ChangeIndex(1);

    private void ChangeIndex(int delta)
    {
        int newIndex = Mathf.Clamp(currentIndex + delta, 0, currentSprites.Count - 1);
        
        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            UpdateArrows();
            MoveToIndex(currentIndex);
            UpdateThumbnailAlpha();
        }
    }
    private void MoveToIndex(int index)
    {
        if (mainImageScrollRect == null) return;
        float childWidth = mainImageContent.childCount > 0 ? mainImageContent.GetChild(0).GetComponent<RectTransform>().rect.width : 600;
        Vector2 pos = mainImageContent.GetComponent<RectTransform>().anchoredPosition;
        pos.x = -index * (childWidth + GetSpacing());
        DOTween.To(() => mainImageContent.GetComponent<RectTransform>().anchoredPosition, x => mainImageContent.GetComponent<RectTransform>().anchoredPosition = x, pos, 0.3f).SetEase(Ease.OutQuad);
    }

    private void UpdateThumbnailAlpha() 
    {
        // 遍歷 smallImageContent 中的所有子物件（縮略圖按鈕）
        for (int i = 0; i < smallImageContent.childCount; i++)
        {
            GameObject thumbnail = smallImageContent.GetChild(i).gameObject;
            // 從按鈕的子物件中取得名為 "Image" 的元件
            Transform imgTransform = thumbnail.transform.Find("Image");
            if (imgTransform != null)
            {
                Image img = imgTransform.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    // 當前選取的縮略圖設為完全不透明，其餘為半透明
                    c.a = (i == currentIndex) ? 1f : 0.5f;
                    img.color = c;
                }
            }
        }
    }



    private float GetSpacing()
    {
        HorizontalLayoutGroup layout = mainImageContent.GetComponent<HorizontalLayoutGroup>();
        return layout != null ? layout.spacing : 0;
    }

    public void Hide(bool model)
    {
        panelRoot.SetActive(false);
        ProductListManager.Instance.OnClicklist();
        if (model == false)
        {
            ProductListManager.Instance.OnClickToggleImage();
            mainImageScrollRect.horizontalNormalizedPosition = 0;
        }
    }
}