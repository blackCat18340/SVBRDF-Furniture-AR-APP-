using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARFurniture
{
    // 自製光照估測：讀 AR 相機影像的「平均亮度 + 顏色」，驅動方向光 + 環境光，讓虛擬物貼合現場明暗/色偏。
    // 不依賴 ARCore 光估測子系統(Lightship 沒提供)，只讀相機影像 → 一定跑得起來。
    // 反射(interior.exr)不動，這支只管整體曝光/色彩。裝置上才有相機影像；編輯器可能取不到(會自動略過)。
    public class CameraLightEstimation : MonoBehaviour
    {
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Light directionalLight;

        [Header("取樣")]
        [SerializeField] private float updateInterval = 0.2f;        // 多久取樣一次(秒)，省效能

        [Header("亮度")]
        [SerializeField] private float lightIntensityMultiplier = 1.2f; // 亮房間→更亮的主光(調低=整體變暗)
        [SerializeField] private float minLightIntensity = 0.1f;
        [SerializeField] private float maxLightIntensity = 1.0f;

        [Header("顏色 / 環境光")]
        [SerializeField, Range(0f, 1f)] private float colorInfluence = 0.75f; // 套用房間色偏的強度
        [SerializeField] private bool driveAmbient = true;
        [SerializeField] private float ambientMultiplier = 1f;

        [Header("反射強度(跟著亮度調，避免暗房間金屬還是亮)")]
        [SerializeField] private bool driveReflection = true;
        [SerializeField] private float reflectionMultiplier = 2f;
        [SerializeField] private float minReflection = 0.05f;
        [SerializeField] private float maxReflection = 1f;

        [Header("陰影強度(跟著亮度調：暗房間陰影變淡)")]
        [SerializeField] private bool driveShadow = true;
        [SerializeField] private float shadowMultiplier = 2.5f;
        [SerializeField] private float minShadow = 0.15f;
        [SerializeField] private float maxShadow = 1f;

        [Header("平滑")]
        [SerializeField] private float smoothSpeed = 4f;             // 越大越快跟上，越小越穩(防閃)

        private float timer;
        private float targetIntensity = 1f;
        private Color targetColor = Color.white;
        private float targetReflection = 1f;
        private float targetShadow = 1f;
        private float curShadow = 1f;
        private static readonly int ShadowStrengthMulID = Shader.PropertyToID("_ShadowStrengthMul");

        void Awake()
        {
            if (cameraManager == null) cameraManager = FindObjectOfType<ARCameraManager>();
            if (directionalLight == null)
                foreach (var l in FindObjectsOfType<Light>())
                    if (l.type == LightType.Directional) { directionalLight = l; break; }
            if (directionalLight != null) directionalLight.useColorTemperature = false; // 直接給 color
            if (driveAmbient) RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            Shader.SetGlobalFloat(ShadowStrengthMulID, 1f); // 先給安全預設，避免一開始陰影消失
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval) { timer = 0f; Sample(); }

            float k = Time.deltaTime * smoothSpeed;
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, k);
                directionalLight.color = Color.Lerp(directionalLight.color, targetColor, k);
            }
            if (driveAmbient)
                RenderSettings.ambientLight = Color.Lerp(
                    RenderSettings.ambientLight,
                    targetColor * (targetIntensity * 0.5f * ambientMultiplier), k);

            if (driveReflection)
                RenderSettings.reflectionIntensity = Mathf.Lerp(
                    RenderSettings.reflectionIntensity, targetReflection, k);

            if (driveShadow)
            {
                curShadow = Mathf.Lerp(curShadow, targetShadow, k);
                Shader.SetGlobalFloat(ShadowStrengthMulID, curShadow);
            }
        }

        void Sample()
        {
            if (cameraManager == null) return;
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

            using (image)
            {
                var p = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(16, 16),      // 縮到很小再平均，超便宜
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None
                };
                int size = image.GetConvertedDataSize(p);
                var buf = new NativeArray<byte>(size, Allocator.Temp);
                try
                {
                    image.Convert(p, buf);
                    long r = 0, g = 0, b = 0; int n = size / 4;
                    for (int i = 0; i < size; i += 4) { r += buf[i]; g += buf[i + 1]; b += buf[i + 2]; }
                    float ar = r / (255f * n), ag = g / (255f * n), ab = b / (255f * n);
                    float lum = 0.2126f * ar + 0.7152f * ag + 0.0722f * ab;

                    targetIntensity = Mathf.Clamp(lum * lightIntensityMultiplier, minLightIntensity, maxLightIntensity);
                    targetReflection = Mathf.Clamp(lum * reflectionMultiplier, minReflection, maxReflection);
                    targetShadow = Mathf.Clamp(lum * shadowMultiplier, minShadow, maxShadow);

                    // 房間色偏：正規化到最大通道=1(只取色相、不再變暗)，再往白色 blend
                    float mx = Mathf.Max(ar, Mathf.Max(ag, ab), 0.001f);
                    Color tint = new Color(ar / mx, ag / mx, ab / mx, 1f);
                    targetColor = Color.Lerp(Color.white, tint, colorInfluence);
                }
                finally { buf.Dispose(); }
            }
        }
    }
}
