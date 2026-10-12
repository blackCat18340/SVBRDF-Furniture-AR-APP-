using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
namespace UISwitcher {
    public class SwitchARPlaneMaterial : MonoBehaviour
    {
        // ARPlaneManager 參考，請在 Inspector 指派
        public ARPlaneManager arPlaneManager;
        // 要切換的兩個材質
        public Material materialA;
        public Material materialB;
        // 目前是否使用 materialA
        private bool usingMaterialA = true;
        public GameObject FPSPanel;

        // 按鈕參考，請在 Inspector 指派
        [SerializeField] private UISwitcher ARPlaneSwitcher;
        [SerializeField] private UISwitcher FPSSwitcher;

        private void Awake(){
            if (arPlaneManager.planePrefab != null)
            {
                MeshRenderer prefabMR = arPlaneManager.planePrefab.GetComponent<MeshRenderer>();
                if (prefabMR != null){
                    prefabMR.material = materialA;
                }
            }
            // 設定按鈕點擊事件
            ARPlaneSwitcher.onValueChanged.AddListener(SwitchMaterial);
            FPSSwitcher.onValueChanged.AddListener(SwitchFPS);
        }
        // 切換材質的方法
        public void SwitchMaterial(bool isOn){
            Material newMaterial = usingMaterialA ? materialB : materialA;
            usingMaterialA = !usingMaterialA;

            // 更新所有現有的 AR Plane
            foreach (ARPlane plane in arPlaneManager.trackables){
                MeshRenderer mr = plane.GetComponent<MeshRenderer>();
                if(mr != null){
                    mr.material = newMaterial;
                }
            }

            // 更新 ARPlaneManager 的 planePrefab
            if (arPlaneManager.planePrefab != null){
                MeshRenderer prefabMR = arPlaneManager.planePrefab.GetComponent<MeshRenderer>();
                if (prefabMR != null){
                    prefabMR.material = newMaterial;
                }
            }
        }
        public void SwitchFPS(bool isOn){
            if(FPSSwitcher.isOn){
                FPSPanel.SetActive(true);
            }else{
                FPSPanel.SetActive(false);
            }
        }
    }
}
