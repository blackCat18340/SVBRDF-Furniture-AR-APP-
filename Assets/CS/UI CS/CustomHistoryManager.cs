using UnityEngine;
using UnityEngine.UI;

public class CustomHistoryManager : MonoBehaviour
{
    public GameObject historyPanel;
    public GameObject productListPanel;
    public Button customizeHistoryButton;

    private void Start()
    {
        if (historyPanel != null)
        {
            historyPanel.SetActive(false);
        }

        if (productListPanel != null)
        {
            productListPanel.SetActive(true);
        }

        if (customizeHistoryButton != null)
        {
            customizeHistoryButton.gameObject.SetActive(false);
        }
    }
}
