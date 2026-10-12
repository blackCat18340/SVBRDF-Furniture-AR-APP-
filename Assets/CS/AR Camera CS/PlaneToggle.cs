using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlaneToggle : MonoBehaviour
{
    public Button toggleButton;
    public ARPlaneManager planeManager;

    private bool planesVisible = true;

    void Start()
    {
        toggleButton.onClick.AddListener(TogglePlanes);
    }

    void TogglePlanes()
    {
        planesVisible = !planesVisible;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(planesVisible);
        }
    }
}
