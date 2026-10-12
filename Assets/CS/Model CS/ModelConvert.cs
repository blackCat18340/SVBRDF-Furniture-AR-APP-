using UnityEngine;
using UnityEngine.UI;

public class ModelConverter : MonoBehaviour
{
    public GameObject customizePanel;
    public Button customizeHistoryButton;

    [System.Serializable]
    public class ConversionData
    {
        public string id;
        public string part;
        public string prompt;
    }

    private void Start()
    {
        DisableCustomizeUI();
    }

    public void SetCurrentModelId(string modelId)
    {
        Debug.Log("Custom model conversion is disabled in local-only mode.");
        DisableCustomizeUI();
    }

    public void UpdateHistoryButtonVisibility()
    {
        DisableCustomizeUI();
    }

    private void DisableCustomizeUI()
    {
        if (customizePanel != null)
        {
            customizePanel.SetActive(false);
        }

        if (customizeHistoryButton != null)
        {
            customizeHistoryButton.gameObject.SetActive(false);
        }
    }
}
