using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchDialogManager : MonoBehaviour
{
    [Header("Local Search UI")]
    public GameObject searchDialog;
    public Dropdown categoryDropdown;
    public Button confirmButton;
    public Button cancelButton;
    public Button searchButton;
    public InputField searchInputField;
    public Button clearSearchButton;
    public Button reloadButton;

    private ProductListManager productListManager;

    private void Start()
    {
        productListManager = FindObjectOfType<ProductListManager>();

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmSearch);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelSearch);
        if (searchButton != null) searchButton.onClick.AddListener(OpenSearchDialog);
        if (clearSearchButton != null) clearSearchButton.onClick.AddListener(ClearSearchResults);
        if (reloadButton != null) reloadButton.onClick.AddListener(ReloadCurrentPage);

        if (searchDialog != null)
        {
            searchDialog.SetActive(false);
        }
    }

    public void OpenSearchDialog()
    {
        if (searchDialog != null)
        {
            searchDialog.SetActive(true);
        }
    }

    public void OnCancelSearch()
    {
        if (searchDialog != null)
        {
            searchDialog.SetActive(false);
        }
    }

    public void OnConfirmSearch()
    {
        if (productListManager == null) return;

        string query = searchInputField != null ? searchInputField.text.Trim().ToLowerInvariant() : "";
        if (string.IsNullOrEmpty(query))
        {
            productListManager.ResetToAllProducts();
            OnCancelSearch();
            return;
        }

        List<ProductData> results = productListManager.SearchLocalProducts(query);
        productListManager.SetSearchResults(results, query);
        OnCancelSearch();
    }

    public void ClearSearchResults()
    {
        if (searchInputField != null)
        {
            searchInputField.text = "";
        }

        if (productListManager != null)
        {
            productListManager.ResetToAllProducts();
        }
    }

    public void ReloadCurrentPage()
    {
        if (productListManager != null)
        {
            productListManager.ReloadLocalProducts();
        }

        OnCancelSearch();
    }

}
