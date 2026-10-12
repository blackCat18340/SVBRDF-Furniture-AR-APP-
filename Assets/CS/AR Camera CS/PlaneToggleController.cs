using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class PlaneToggleController : MonoBehaviour
{
    public ARPlaneManager arPlaneManager; // 指向 ARPlaneManager
    public Button toggleButton; // 指向 UI 按鈕
    private bool planesVisible = true;

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePlanes);
        }
    }

    void TogglePlanes()
    {
        planesVisible = !planesVisible;
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(planesVisible);
        }
    }
}
