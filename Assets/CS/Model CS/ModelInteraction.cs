using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModelInteraction : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    private Material originalMaterial;
    private Material glowMaterial;
    private bool isGlowing = false;
    private Camera mainCamera;
    // 用於拖曳時記錄初始偏移
    private Vector3 dragOffset;

    void Start()
    {
        mainCamera = Camera.main;
        // 假設模型至少有一個 Renderer
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
            glowMaterial = new Material(originalMaterial);
            // 啟用發光效果（需確認 shader 支援 emission）
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.SetColor("_EmissionColor", Color.yellow);
        }
    }

    /// <summary>
    /// 點擊模型時切換發光效果
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleGlow();
    }

    private void ToggleGlow()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        if (!isGlowing)
        {
            renderer.material = glowMaterial;
            isGlowing = true;
        }
        else
        {
            renderer.material = originalMaterial;
            isGlowing = false;
        }
    }

    /// <summary>
    /// 拖曳時更新模型位置與旋轉（簡單示範：依據水平滑動旋轉、拖曳移動）
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // 依據滑動量旋轉（此處僅以 X 軸水平滑動作為旋轉參數）
        float rotationSpeed = 0.2f;
        float rotationY = eventData.delta.x * rotationSpeed;
        transform.Rotate(Vector3.up, rotationY, Space.World);

        // 更新物件位置
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 計算拖曳後的位移（可根據需求調整）
            Vector3 newPos = hit.point;
            transform.position = newPos;
        }
    }
}

