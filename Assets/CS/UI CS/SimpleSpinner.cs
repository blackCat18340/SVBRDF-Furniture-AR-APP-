using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Assets.SimpleSpinner
{
    [RequireComponent(typeof(Image))]
    
    public class SimpleSpinner : MonoBehaviour
    {

        [Header("Rotation")]
        public bool Rotation = true;
        [Range(-10, 10), Tooltip("Value in Hz (revolutions per second).")]
        public float RotationSpeed = 1;
        public AnimationCurve RotationAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Rainbow")]
        public bool Rainbow = true;
        [Range(-10, 10), Tooltip("Value in Hz (revolutions per second).")]
        public float RainbowSpeed = 0.5f;
        [Range(0, 1)]
        public float RainbowSaturation = 1f;
        public AnimationCurve RainbowAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Options")]
        public bool RandomPeriod = true;

        [Header("Loading_text")]
        public TMP_Text Loading_text;

        private Image _image;
        private float _period;
        private int dotCount = 0;
        private string baseText = "Loading";

        public void Start()
        {
            _image = GetComponent<Image>();
            _period = RandomPeriod ? Random.Range(0f, 1f) : 0;
            StartCoroutine(UpdateLoadingText());
        }

        public void Update()
        {
            if (Rotation)
            {
                transform.localEulerAngles = new Vector3(0, 0, -360 * RotationAnimationCurve.Evaluate((RotationSpeed * Time.time + _period) % 1));
            }

            if (Rainbow)
            {
                _image.color = Color.HSVToRGB(RainbowAnimationCurve.Evaluate((RainbowSpeed * Time.time + _period) % 1), RainbowSaturation, 1);
            }
        }
        IEnumerator UpdateLoadingText()
        {
            while (true)
            {
                dotCount = (dotCount + 1) % 4; // 讓 . 的數量在 0~3 之間循環
                Loading_text.text = baseText + new string('.', dotCount);
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}