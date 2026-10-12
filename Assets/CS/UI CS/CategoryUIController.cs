using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryUIController : MonoBehaviour
{
    [Header("上方 4 個按鈕")]
    public Button chairBtn;
    public Button sofaBtn;
    public Button deskBtn;
    public Button drawerBtn;

    [Header("下方 4 個容器(Panels)")]
    public GameObject chairPanel;
    public GameObject sofaPanel;
    public GameObject deskPanel;
    public GameObject drawerPanel;

    private Button[] allButtons;
    private GameObject[] allPanels;
    private int currentIndex = 0; // 0=Chair,1=Sofa,2=Desk,3=Drawer

    void Start()
    {
        // 收集按鈕到陣列方便處理
        allButtons = new Button[] { chairBtn, sofaBtn, deskBtn, drawerBtn };
        allPanels = new GameObject[] { chairPanel, sofaPanel, deskPanel, drawerPanel };

        // 註冊按鈕事件
        chairBtn.onClick.AddListener(() => OnClickCategory(0));
        sofaBtn.onClick.AddListener(() => OnClickCategory(1));
        deskBtn.onClick.AddListener(() => OnClickCategory(2));
        drawerBtn.onClick.AddListener(() => OnClickCategory(3));

        // 一開始就顯示第0個(Chair)，隱藏其他
        ShowPanel(0);
        UpdateButtonStyle(0);
    }

    private void OnClickCategory(int index)
    {
        currentIndex = index;
        ShowPanel(index);
        UpdateButtonStyle(index);
    }

    private void ShowPanel(int index)
    {
        // 全部隱藏
        for (int i = 0; i < allPanels.Length; i++)
        {
            allPanels[i].SetActive(false);
        }
        // 顯示指定
        allPanels[index].SetActive(true);
    }

    private void UpdateButtonStyle(int selectedIndex)
    {
        // 4 個按鈕 (0=Chair,1=Sofa,2=Desk,3=Drawer)
        for (int i = 0; i < allButtons.Length; i++)
        {
            var rt = allButtons[i].GetComponent<RectTransform>();
            var img = allButtons[i].GetComponent<Image>();
            var txt = allButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (i == selectedIndex)
            {
                // 被選中
                rt.sizeDelta = new Vector2(400, 180);
                img.color = new Color32(46, 0, 149, 255);
                txt.color = Color.white;
            }
            else
            {
                // 未被選中
                rt.sizeDelta = new Vector2(350, 150);
                img.color = Color.white;
                txt.color = Color.black;
            }
        }
    }
}

