using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ARFurniture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ProductData
{
    public string modelURL;
    public string productName;
    public float price;
    public string url;
    public string otherInfo;
    public Sprite productImage;
    public List<Sprite> allSprites = new List<Sprite>();
    public string sizeOptions;
    public bool from;
    public string productId;
}

[System.Serializable]
public class LocalProductInfo
{
    public string productName;
    public float price;
    public string url;
    public string description;
    public string sizeOptions;
}

public class ProductListManager : MonoBehaviour
{
    public static ProductListManager Instance { get; private set; }

    [Header("Local Model Source")]
    [SerializeField] private string localModelRoot = "3D_Model";
    [SerializeField] private string modelFolderName = "3d_model";
    [SerializeField] private string imageFolderName = "PICS";
    [SerializeField] private string infoFileName = "Info.json";

    [Header("Product Item UI")]
    public GameObject productItemPrefab;
    public Transform productContent;
    public GameObject panelRoot;

    [Header("List Toggle UI")]
    public Button listBtn;
    public GameObject listBtnImage;
    public bool activeSelf = true;
    public Sprite upSprite;
    public Sprite downSprite;

    public List<ProductData> allProducts = new List<ProductData>();
    public ModelLoader1 modelLoader1;

    [Header("Loading")]
    public GameObject loadingPanel;

    [Header("Grid Layout")]
    [SerializeField] private Vector2 cellSize = new Vector2(300f, 350f);
    [SerializeField] private Vector2 spacing = new Vector2(20f, 20f);
    [SerializeField] private int paddingLeft = 20;
    [SerializeField] private int paddingRight = 20;
    [SerializeField] private int paddingTop = 20;
    [SerializeField] private int paddingBottom = 20;
    [SerializeField] private bool useLocalListLayoutDefaults = true;
    [SerializeField] private bool usePrefabSizeForGrid = true;
    [SerializeField] private Vector2 localListSpacing = new Vector2(6f, 20f);
    [SerializeField] private int localListPadding = 0;
    [SerializeField] private int gridColumnCount = 2;
    [SerializeField] private float productImageFitSize = 400f;

    [Header("Pagination")]
    public int itemsPerPage = 20;
    public Button previousPageBtn;
    public Button nextPageBtn;
    public Text pageInfoText;
    public Text itemCountText;

    private readonly List<ProductData> allLocalProducts = new List<ProductData>();
    private readonly List<ProductData> displayedProducts = new List<ProductData>();
    private int currentPage = 1;
    private int totalPages = 0;
    private int totalItemCount = 0;
    private bool isLoadingPage = false;
    private ScrollRect resolvedScrollRect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ResolveUIReferences();

        if (listBtn != null)
        {
            listBtn.image.sprite = downSprite;
            listBtn.onClick.AddListener(OnClickToggleImage);
        }

        SetupPaginationButtons();
        ReloadLocalProducts();
    }

    private void SetupPaginationButtons()
    {
        if (previousPageBtn != null)
        {
            previousPageBtn.onClick.AddListener(LoadPreviousPage);
        }

        if (nextPageBtn != null)
        {
            nextPageBtn.onClick.AddListener(LoadNextPage);
        }
    }

    public void LoadPreviousPage()
    {
        if (isLoadingPage || currentPage <= 1) return;
        currentPage--;
        LoadCurrentPage();
    }

    public void LoadNextPage()
    {
        if (isLoadingPage || currentPage >= totalPages) return;
        currentPage++;
        LoadCurrentPage();
    }

    public void GoToPage(int pageNumber)
    {
        if (isLoadingPage || pageNumber < 1 || pageNumber > totalPages) return;
        currentPage = pageNumber;
        LoadCurrentPage();
    }

    public void ReloadLocalProducts()
    {
        StartCoroutine(ReloadLocalProductsRoutine());
    }

    // 從 GitHub repo(PBR_3D) 取得產品清單：GitHub API 列出檔案樹 → 每個產品讀 Info.json、圖片(快取, 支援 WebP)。
    private IEnumerator ReloadLocalProductsRoutine()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        allLocalProducts.Clear();

        Task<List<RemoteContent.RepoProduct>> listTask = RemoteContent.ListProductsAsync();
        while (!listTask.IsCompleted) yield return null;

        List<RemoteContent.RepoProduct> repoProducts = listTask.Result;
        if (repoProducts == null || repoProducts.Count == 0)
        {
            Debug.LogWarning("[ProductList] GitHub 取不到產品(檢查網路/repo 是否有內容)");
            displayedProducts.Clear();
            totalItemCount = 0;
            totalPages = 0;
            currentPage = 1;
            LoadCurrentPage();
            yield break;
        }

        foreach (RemoteContent.RepoProduct rp in repoProducts)
        {
            ProductData product = new ProductData
            {
                modelURL = RemoteContent.RawUrl(rp.modelPath),   // 模型用 raw URL，載入時下載+快取
                productName = rp.id,
                from = false,
                productId = rp.id
            };

            // Info.json：每次抓新的(不快取)，新上架/改尺寸才會即時反映
            if (!string.IsNullOrEmpty(rp.infoPath))
            {
                Task<byte[]> infoTask = RemoteContent.GetContentAsync(rp.infoPath, cache: false);
                while (!infoTask.IsCompleted) yield return null;
                if (infoTask.Result != null) ApplyInfoJson(Encoding.UTF8.GetString(infoTask.Result), product);
            }

            // 圖片：快取，WebP 由 ImageLoader 在 Android 原生解碼
            foreach (string imgPath in rp.imagePaths)
            {
                Task<byte[]> imgTask = RemoteContent.GetContentAsync(imgPath, cache: true);
                while (!imgTask.IsCompleted) yield return null;

                Sprite sprite = ImageLoader.SpriteFromBytes(imgTask.Result, Path.GetFileNameWithoutExtension(imgPath));
                if (sprite == null) continue;

                product.allSprites.Add(sprite);
                if (product.productImage == null) product.productImage = sprite;
            }

            allLocalProducts.Add(product);
        }

        totalItemCount = allLocalProducts.Count;
        totalPages = Mathf.CeilToInt((float)totalItemCount / Mathf.Max(1, itemsPerPage));
        displayedProducts.Clear();
        displayedProducts.AddRange(allLocalProducts);
        currentPage = totalPages > 0 ? 1 : 0;
        LoadCurrentPage();
    }

    // 把 Info.json 套到 ProductData
    private static void ApplyInfoJson(string json, ProductData product)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            LocalProductInfo info = JsonUtility.FromJson<LocalProductInfo>(json);
            if (info == null) return;
            if (!string.IsNullOrWhiteSpace(info.productName)) product.productName = info.productName.Trim();
            if (info.price > 0f) product.price = info.price;
            if (!string.IsNullOrWhiteSpace(info.url)) product.url = info.url.Trim();
            if (!string.IsNullOrWhiteSpace(info.description)) product.otherInfo = info.description.Trim();
            if (!string.IsNullOrWhiteSpace(info.sizeOptions)) product.sizeOptions = info.sizeOptions.Trim();
        }
        catch (System.Exception e) { Debug.LogWarning($"[ProductList] 解析 Info.json 失敗: {e.Message}"); }
    }

    private static Sprite SpriteFromBytes(byte[] data, string name)
    {
        if (data == null) return null;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(data))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }
        texture.name = name;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void ResetToAllProducts()
    {
        ReloadLocalProducts();
    }

    public void SetSearchResults(List<ProductData> searchResults, string label)
    {
        displayedProducts.Clear();
        if (searchResults != null)
        {
            displayedProducts.AddRange(searchResults);
        }

        totalItemCount = displayedProducts.Count;
        totalPages = Mathf.CeilToInt((float)totalItemCount / Mathf.Max(1, itemsPerPage));
        currentPage = totalPages > 0 ? 1 : 0;
        LoadCurrentPage();
    }

    public List<ProductData> SearchLocalProducts(string query)
    {
        List<ProductData> results = new List<ProductData>();
        string normalizedQuery = string.IsNullOrWhiteSpace(query) ? "" : query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            results.AddRange(allLocalProducts);
            return results;
        }

        foreach (ProductData product in allLocalProducts)
        {
            if (Contains(product.productName, normalizedQuery)
                || Contains(product.productId, normalizedQuery)
                || Contains(product.otherInfo, normalizedQuery)
                || Contains(product.sizeOptions, normalizedQuery))
            {
                results.Add(product);
            }
        }

        return results;
    }

    private void LoadCurrentPage()
    {
        if (isLoadingPage) return;
        isLoadingPage = true;

        ResolveUIReferences();
        ClearCurrentProducts();

        if (totalItemCount > 0)
        {
            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, displayedProducts.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                allProducts.Add(displayedProducts[i]);
            }
        }

        CreateProductItems();
        UpdatePaginationUI();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        isLoadingPage = false;
    }

    private string GetLocalModelRootPath()
    {
        string normalized = localModelRoot.Replace('\\', '/').Trim('/');
        if (Path.IsPathRooted(localModelRoot))
        {
            return localModelRoot;
        }

        if (normalized.StartsWith("Assets/"))
        {
            normalized = normalized.Substring("Assets/".Length);
        }

        return Path.Combine(Application.dataPath, normalized);
    }

    private ProductData CreateProductFromFolder(string productFolder)
    {
        string modelFolder = Path.Combine(productFolder, modelFolderName);
        string[] modelFiles = Directory.Exists(modelFolder)
            ? Directory.GetFiles(modelFolder, "*.glb", SearchOption.TopDirectoryOnly)
            : Directory.GetFiles(productFolder, "*.glb", SearchOption.AllDirectories);

        if (modelFiles.Length == 0)
        {
            Debug.LogWarning($"No GLB model found in {productFolder}");
            return null;
        }

        string productName = ObjectNameFromFolder(productFolder);
        float price = 0f;
        string url = "";
        string description = "";
        string size = "";

        ReadInfoFile(GetInfoFilePath(productFolder), ref productName, ref price, ref url, ref description, ref size);

        ProductData product = new ProductData
        {
            modelURL = modelFiles[0],
            productName = productName,
            price = price,
            url = url,
            otherInfo = description,
            sizeOptions = size,
            from = false,
            productId = Path.GetFileName(productFolder)
        };

        LoadProductImages(productFolder, product);
        return product;
    }

    private static string ObjectNameFromFolder(string folder)
    {
        return Path.GetFileName(folder).Replace('_', ' ');
    }

    private string GetInfoFilePath(string productFolder)
    {
        string configuredPath = Path.Combine(productFolder, infoFileName);
        if (File.Exists(configuredPath))
        {
            return configuredPath;
        }

        string jsonPath = Path.Combine(productFolder, "Info.json");
        if (File.Exists(jsonPath))
        {
            return jsonPath;
        }

        string txtPath = Path.Combine(productFolder, "Info.txt");
        return File.Exists(txtPath) ? txtPath : "";
    }

    private void ReadInfoFile(string infoPath, ref string productName, ref float price, ref string url, ref string description, ref string size)
    {
        if (!File.Exists(infoPath)) return;

        string text = File.ReadAllText(infoPath).Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (text.StartsWith("{"))
        {
            try
            {
                LocalProductInfo info = JsonUtility.FromJson<LocalProductInfo>(text);
                if (info == null) return;

                if (!string.IsNullOrWhiteSpace(info.productName)) productName = info.productName.Trim();
                if (info.price > 0f) price = info.price;
                if (!string.IsNullOrWhiteSpace(info.url)) url = info.url.Trim();
                if (!string.IsNullOrWhiteSpace(info.description)) description = info.description.Trim();
                if (!string.IsNullOrWhiteSpace(info.sizeOptions)) size = info.sizeOptions.Trim();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to parse product info JSON: {infoPath}. {e.Message}");
            }

            return;
        }

        string[] lines = text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])) productName = lines[0].Trim();
        if (lines.Length > 1) float.TryParse(lines[1], NumberStyles.Float, CultureInfo.InvariantCulture, out price);
        if (lines.Length > 2) url = lines[2].Trim();
        if (lines.Length > 3) description = lines[3].Trim();
        if (lines.Length > 4) size = lines[4].Trim();
    }

    private void LoadProductImages(string productFolder, ProductData product)
    {
        string imageFolder = Path.Combine(productFolder, imageFolderName);
        if (!Directory.Exists(imageFolder))
        {
            imageFolder = productFolder;
        }

        string[] imageFiles = Directory.GetFiles(imageFolder, "*.*", SearchOption.TopDirectoryOnly);
        foreach (string imageFile in imageFiles)
        {
            string ext = Path.GetExtension(imageFile).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

            Sprite sprite = LoadSpriteFromFile(imageFile);
            if (sprite == null) continue;

            product.allSprites.Add(sprite);
            if (product.productImage == null)
            {
                product.productImage = sprite;
            }
        }
    }

    private static Sprite LoadSpriteFromFile(string imagePath)
    {
        byte[] data = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(data))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(imagePath);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void UpdatePaginationUI()
    {
        if (previousPageBtn != null)
        {
            previousPageBtn.interactable = currentPage > 1;
        }

        if (nextPageBtn != null)
        {
            nextPageBtn.interactable = currentPage < totalPages;
        }

        if (pageInfoText != null)
        {
            pageInfoText.text = totalPages > 0 ? $"{currentPage} / {totalPages}" : "0 / 0";
        }

        if (itemCountText != null)
        {
            int currentPageItems = allProducts.Count;
            itemCountText.text = $"{currentPageItems} / {totalItemCount}";
        }
    }

    private void ClearCurrentProducts()
    {
        allProducts.Clear();

        ResolveUIReferences();
        if (productContent == null) return;

        foreach (Transform child in productContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void SetupGridLayout()
    {
        ResolveUIReferences();
        if (productContent == null || productContent.gameObject == null)
        {
            Debug.LogWarning("ProductListManager has no productContent. Product UI will not be created.");
            return;
        }

        RemoveLayoutGroup<VerticalLayoutGroup>();
        RemoveLayoutGroup<HorizontalLayoutGroup>();

        var existingGrid = productContent.GetComponent<GridLayoutGroup>();
        GridLayoutGroup gridLayout = existingGrid;
        if (gridLayout == null)
        {
            gridLayout = productContent.gameObject.AddComponent<GridLayoutGroup>();
        }

        if (gridLayout == null)
        {
            Debug.LogWarning("ProductListManager could not create GridLayoutGroup on productContent.");
            return;
        }

        RectTransform contentRect = productContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(0f, contentRect.sizeDelta.y));
        }

        Vector2 effectiveCellSize = GetGridCellSize();
        Vector2 effectiveSpacing = useLocalListLayoutDefaults ? localListSpacing : spacing;
        RectOffset effectivePadding = useLocalListLayoutDefaults
            ? new RectOffset(localListPadding, localListPadding, localListPadding, localListPadding)
            : new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

        gridLayout.cellSize = effectiveCellSize;
        gridLayout.spacing = effectiveSpacing;
        gridLayout.padding = effectivePadding;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = GetGridColumnCount(effectiveCellSize, effectiveSpacing, effectivePadding);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        ContentSizeFitter fitter = productContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = productContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    public void CreateProductItems()
    {
        ResolveUIReferences();
        if (productItemPrefab == null || productContent == null) return;

        SetupGridLayout();

        foreach (ProductData product in allProducts)
        {
            GameObject itemObj = Instantiate(productItemPrefab, productContent);

            RectTransform btnRect = itemObj.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.sizeDelta = GetGridCellSize();
            }

            Image image = itemObj.transform.Find("ProductImage/Image")?.GetComponent<Image>();
            Transform productImageRoot = itemObj.transform.Find("ProductImage");
            Text nameText = itemObj.transform.Find("NameText")?.GetComponentInChildren<Text>();
            TextMeshProUGUI priceText = itemObj.transform.Find("PriceText")?.GetComponentInChildren<TextMeshProUGUI>();
            Button arButton = itemObj.transform.Find("ViewARButton")?.GetComponent<Button>();
            Button infoButton = itemObj.transform.Find("InfoButton")?.GetComponent<Button>();
            ProductData capturedProduct = product;

            if (image != null && product.productImage != null)
            {
                image.sprite = product.productImage;
                FitImage(image, product.productImage, productImageFitSize);
            }

            AddLoadModelClick(productImageRoot, capturedProduct);
            AddLoadModelClick(image != null ? image.transform : null, capturedProduct);

            if (nameText != null)
            {
                nameText.text = product.productName;
            }

            if (priceText != null)
            {
                priceText.text = product.price > 0f ? "$" + product.price.ToString("F2", CultureInfo.InvariantCulture) : "";
            }

            if (infoButton != null)
            {
                infoButton.onClick.AddListener(() =>
                {
                    OnClickToggleImage();
                    if (InfoPanelController.Instance != null)
                    {
                        InfoPanelController.Instance.ShowProductInfo(capturedProduct);
                    }
                    else
                    {
                        Debug.LogWarning("InfoPanelController is not available in the scene.");
                    }
                });
            }

            if (arButton != null)
            {
                arButton.onClick.AddListener(() =>
                {
                    OnClickToggleImage();
                    LoadProductModel(capturedProduct);
                });
            }
        }

        ResizeProductContent();
        Canvas.ForceUpdateCanvases();

        if (resolvedScrollRect != null)
        {
            resolvedScrollRect.StopMovement();
            resolvedScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void AddLoadModelClick(Transform target, ProductData product)
    {
        if (target == null) return;

        Image targetImage = target.GetComponent<Image>();
        if (targetImage != null)
        {
            targetImage.raycastTarget = true;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            OnClickToggleImage();
            LoadProductModel(product);
        });
    }

    private void LoadProductModel(ProductData product)
    {
        if (modelLoader1 != null)
        {
            modelLoader1.SetModelToLoad(product.modelURL, product);
        }
        else
        {
            Debug.LogWarning("ProductListManager has no ModelLoader1 reference.");
        }
    }

    private void ResizeProductContent()
    {
        RectTransform contentRect = productContent as RectTransform;
        if (contentRect == null) return;

        Vector2 effectiveCellSize = GetGridCellSize();
        Vector2 effectiveSpacing = useLocalListLayoutDefaults ? localListSpacing : spacing;
        RectOffset effectivePadding = useLocalListLayoutDefaults
            ? new RectOffset(localListPadding, localListPadding, localListPadding, localListPadding)
            : new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        int columns = GetGridColumnCount(effectiveCellSize, effectiveSpacing, effectivePadding);
        int rows = Mathf.CeilToInt(allProducts.Count / (float)Mathf.Max(1, columns));
        float contentHeight = effectivePadding.vertical;

        if (rows > 0)
        {
            contentHeight += (effectiveCellSize.y * rows) + (effectiveSpacing.y * (rows - 1));
        }

        RectTransform viewport = resolvedScrollRect != null
            ? resolvedScrollRect.viewport
            : contentRect.parent as RectTransform;
        if (viewport != null)
        {
            contentHeight = Mathf.Max(contentHeight, viewport.rect.height);
        }

        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, contentHeight);
        contentRect.anchoredPosition = Vector2.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private static void FitImage(Image image, Sprite sprite, float fixedSize)
    {
        if (image == null || sprite == null || fixedSize <= 0f) return;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;
        if (spriteWidth <= 0f || spriteHeight <= 0f) return;

        float calculatedWidth = fixedSize * (spriteWidth / spriteHeight);
        float calculatedHeight = fixedSize * (spriteHeight / spriteWidth);

        RectTransform imageRect = image.GetComponent<RectTransform>();
        if (imageRect == null) return;

        imageRect.sizeDelta = calculatedWidth > fixedSize
            ? new Vector2(fixedSize, calculatedHeight)
            : new Vector2(calculatedWidth, fixedSize);
        imageRect.anchoredPosition = Vector2.zero;
    }

    private Vector2 GetGridCellSize()
    {
        if (usePrefabSizeForGrid && productItemPrefab != null)
        {
            RectTransform prefabRect = productItemPrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                Vector2 prefabSize = prefabRect.sizeDelta;
                if (prefabSize.x > 0f && prefabSize.y > 0f)
                {
                    return prefabSize;
                }

                Vector2 rectSize = prefabRect.rect.size;
                if (rectSize.x > 0f && rectSize.y > 0f)
                {
                    return rectSize;
                }
            }
        }

        return cellSize;
    }

    private int GetGridColumnCount(Vector2 effectiveCellSize, Vector2 effectiveSpacing, RectOffset effectivePadding)
    {
        int columns = Mathf.Max(1, gridColumnCount);
        RectTransform parentRect = productContent != null ? productContent.parent as RectTransform : null;
        float availableWidth = parentRect != null ? parentRect.rect.width : 0f;
        if (availableWidth <= 0f || effectiveCellSize.x <= 0f)
        {
            return columns;
        }

        while (columns > 1)
        {
            float requiredWidth = effectivePadding.horizontal
                + (effectiveCellSize.x * columns)
                + (effectiveSpacing.x * (columns - 1));

            if (requiredWidth <= availableWidth)
            {
                break;
            }

            columns--;
        }

        return columns;
    }

    private void RemoveLayoutGroup<T>() where T : LayoutGroup
    {
        if (productContent == null) return;

        T layoutGroup = productContent.GetComponent<T>();
        if (layoutGroup == null) return;

        DestroyImmediate(layoutGroup);
    }

    public void OnClickToggleImage()
    {
        if (listBtn == null || panelRoot == null) return;

        Sprite currentSprite = listBtn.image.sprite;
        if (currentSprite == upSprite)
        {
            listBtn.image.sprite = downSprite;
            panelRoot.SetActive(false);
        }
        else
        {
            listBtn.image.sprite = upSprite;
            panelRoot.SetActive(true);
        }
    }

    public void OnClicklist()
    {
        if (listBtnImage != null)
        {
            listBtnImage.SetActive(!listBtnImage.activeSelf);
        }
    }

    private static bool Contains(string value, string query)
    {
        return !string.IsNullOrEmpty(value) && value.ToLowerInvariant().Contains(query);
    }

    private void ResolveUIReferences()
    {
        if (productContent == null)
        {
            Transform root = panelRoot != null ? panelRoot.transform : transform;
            ScrollRect scrollRect = root.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null && scrollRect.content != null)
            {
                resolvedScrollRect = scrollRect;
                productContent = scrollRect.content;
            }
        }

        if (resolvedScrollRect == null && productContent != null)
        {
            resolvedScrollRect = productContent.GetComponentInParent<ScrollRect>();
        }

        if (productContent == null)
        {
            Transform root = panelRoot != null ? panelRoot.transform : transform;
            Transform content = FindChildByName(root, "Content");
            if (content != null)
            {
                productContent = content;
                resolvedScrollRect = productContent.GetComponentInParent<ScrollRect>();
            }
        }
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindChildByName(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
