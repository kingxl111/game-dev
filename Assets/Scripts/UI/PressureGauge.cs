using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReactorBreach.Core;

namespace ReactorBreach.UI
{
    public class PressureGauge : MonoBehaviour
    {
        [SerializeField] private Slider          _slider;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image           _fillImage;
        [SerializeField] private Color           _safeColor    = Color.green;
        [SerializeField] private Color           _dangerColor  = Color.red;

        private LevelManager _levelManager;

        private void Start()
        {
            _levelManager = FindFirstObjectByType<LevelManager>();
            if (_levelManager != null)
                _levelManager.OnPressureChanged += UpdateGauge;

            // Hide if no timer on this level
            bool hasPressure = _levelManager != null && _levelManager.IsTimerActive;
            gameObject.SetActive(hasPressure);
        }

        private void OnDestroy()
        {
            if (_levelManager != null)
                _levelManager.OnPressureChanged -= UpdateGauge;
        }

        private void UpdateGauge(float pressure)
        {
            float t = pressure / 100f;
            if (_slider != null) _slider.value = t;
            if (_label  != null) _label.text   = $"{pressure:F0}%";
            if (_fillImage != null)
                _fillImage.color = Color.Lerp(_dangerColor, _safeColor, t);
        }
    }
}
